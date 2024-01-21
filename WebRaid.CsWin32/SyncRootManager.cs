using System.Runtime.InteropServices;
using System.Security.Principal;
using Vanara.Extensions;
using Vanara.PInvoke;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;
using static Vanara.PInvoke.CldApi;

namespace WebRaid.CsWin32
{
public class SyncRootManager
    {
        public string Pfad=> SyncRootPath;
        internal static readonly string SyncRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "TestCloud");
        internal static readonly Guid StorageProviderId = Guid.Parse("BEE7B8DC-9699-4561-910D-D3B2F6A923A9");

        internal const string CloudName = "Test Cloud";
        internal const string StorageProviderAccount = "SomeAccount";

        private readonly CF_CALLBACK_REGISTRATION[] _callbacks = [
            new CF_CALLBACK_REGISTRATION
            {
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_PLACEHOLDERS,
                Callback = new CF_CALLBACK((in CF_CALLBACK_INFO x, in CF_CALLBACK_PARAMETERS y) => CfExecutePlaceholdersFetch(new FileSystemItem
                {
                    Id = Guid.NewGuid(),
                    RelativePath = "testFile.txt",
                    FileAttributes = System.IO.FileAttributes.Normal,
                    CreationTime = DateTime.Now,
                    LastAccessTime = DateTime.Now,
                    LastWriteTime = DateTime.Now,
                    Size = 15654654
                }, x)),
            },
            new CF_CALLBACK_REGISTRATION
            {
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NONE
            }];

        public CF_CONNECTION_KEY ConnectionKey { get; private set; }

        public async Task Connect()
        {
            EnsureFeatureSupported();
            EnsureSyncRootPathCreated();

            StorageProviderSyncRootManager.Register(await CreateSyncRoot());

            CfConnectSyncRoot(
                SyncRootPath,
                _callbacks,
                nint.Zero,
                CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_PROCESS_INFO | CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_FULL_FILE_PATH,
                out var connectionKey);

            ConnectionKey = connectionKey;

            CfUpdateSyncProviderStatus(ConnectionKey, CF_SYNC_PROVIDER_STATUS.CF_PROVIDER_STATUS_IDLE);
        }

        private static void EnsureFeatureSupported()
        {
            if (!StorageProviderSyncRootManager.IsSupported())
            {
                throw new NotSupportedException("Cloud API is not supported on this machine.");
            }
        }

        private static void EnsureSyncRootPathCreated()
        {
            if (!Directory.Exists(SyncRootPath))
            {
                Directory.CreateDirectory(SyncRootPath);
            }
        }

        private static async Task<StorageProviderSyncRootInfo> CreateSyncRoot()
        {
            StorageProviderSyncRootInfo syncRoot = new()
            {
                Id = GetSyncRootId(),
                ProviderId = StorageProviderId,
                Path = await StorageFolder.GetFolderFromPathAsync(SyncRootPath),
                AllowPinning = true,
                DisplayNameResource = CloudName,
                HardlinkPolicy = StorageProviderHardlinkPolicy.Allowed,
                HydrationPolicy = StorageProviderHydrationPolicy.Partial,
                HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.AutoDehydrationAllowed | StorageProviderHydrationPolicyModifier.StreamingAllowed,
                InSyncPolicy = StorageProviderInSyncPolicy.FileLastWriteTime,
                PopulationPolicy = StorageProviderPopulationPolicy.Full,
                ProtectionMode = StorageProviderProtectionMode.Unknown,
                Version = "1.0.0",
                IconResource = "%SystemRoot%\\system32\\charmap.exe,0",
                ShowSiblingsAsGroup = false,
                RecycleBinUri = null,
                Context = CryptographicBuffer.ConvertStringToBinary(GetSyncRootId(), BinaryStringEncoding.Utf8)
            };

            return syncRoot;

        }

        private static string GetSyncRootId()
            => $"{StorageProviderId}!{WindowsIdentity.GetCurrent().User}!{StorageProviderAccount}";

        #region Placeholder creation
        private static void CfExecutePlaceholdersFetch(FileSystemItem placeholder, CF_CALLBACK_INFO callbackInfo)
        {
            CF_OPERATION_INFO operationInfo = CreateOperationInfo(callbackInfo, CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_PLACEHOLDERS);
            CF_PLACEHOLDER_CREATE_INFO nativePlaceholder = CreatePlaceholder(placeholder);

            IntPtr placeholderArray = Marshal.AllocHGlobal(Marshal.SizeOf<CF_PLACEHOLDER_CREATE_INFO>());
            Marshal.StructureToPtr(nativePlaceholder, placeholderArray, false);

            CF_OPERATION_PARAMETERS.TRANSFERPLACEHOLDERS placeholdersFetchParameter = new()
            {
                PlaceholderArray = placeholderArray,
                Flags = CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAGS.CF_OPERATION_TRANSFER_PLACEHOLDERS_FLAG_DISABLE_ON_DEMAND_POPULATION,
                PlaceholderCount = 1,
                CompletionStatus = NTStatus.STATUS_SUCCESS,
                PlaceholderTotalCount = 1,
            };

            CF_OPERATION_PARAMETERS opParams = CF_OPERATION_PARAMETERS.Create(placeholdersFetchParameter);

            HRESULT result = CfExecute(operationInfo, ref opParams);
            result.ThrowIfFailed();

            Marshal.FreeHGlobal(placeholderArray);
            Marshal.FreeHGlobal(nativePlaceholder.FileIdentity);
        }

        private static CF_PLACEHOLDER_CREATE_INFO CreatePlaceholder(FileSystemItem placeholder)
        {
            CF_PLACEHOLDER_CREATE_INFO cfInfo = new()
            {
                FileIdentity = Marshal.StringToHGlobalUni("a"),
                FileIdentityLength = 2 * 2,
                RelativeFileName = placeholder.RelativePath,
                FsMetadata = new CF_FS_METADATA
                {
                    FileSize = placeholder.Size,
                    BasicInfo = new Kernel32.FILE_BASIC_INFO
                    {
                        FileAttributes = (FileFlagsAndAttributes)placeholder.FileAttributes,
                        CreationTime = FileTimeExtensions.MakeFILETIME((ulong)placeholder.CreationTime.ToFileTime()),
                        LastWriteTime = FileTimeExtensions.MakeFILETIME((ulong)placeholder.LastWriteTime.ToFileTime()),
                        LastAccessTime = FileTimeExtensions.MakeFILETIME((ulong)placeholder.LastAccessTime.ToFileTime()),
                        ChangeTime = FileTimeExtensions.MakeFILETIME((ulong)placeholder.LastWriteTime.ToFileTime())
                    }
                },
                Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC
            };

            return cfInfo;
        }

        private static CF_OPERATION_INFO CreateOperationInfo(in CF_CALLBACK_INFO CallbackInfo, CF_OPERATION_TYPE OperationType)
        {
            CF_OPERATION_INFO opInfo = new()
            {
                Type = OperationType,
                ConnectionKey = CallbackInfo.ConnectionKey,
                TransferKey = CallbackInfo.TransferKey,
                CorrelationVector = CallbackInfo.CorrelationVector,
                RequestKey = CallbackInfo.RequestKey
            };

            opInfo.StructSize = (uint)Marshal.SizeOf(opInfo);
            return opInfo;
        }

        public class FileSystemItem
        {
            public Guid Id { get; set; }
            public string RelativePath { get; set; }
            public long Size { get; set; }
            public System.IO.FileAttributes FileAttributes { get; set; }
            public DateTimeOffset CreationTime { get; set; }
            public DateTimeOffset LastWriteTime { get; set; }
            public DateTimeOffset LastAccessTime { get; set; }
        }
        #endregion
    }
}
