using Microsoft.Extensions.Logging;

namespace WebRaid.VDS
{
    internal static class WildcardMatch
    {
        internal static ILogger Logger = null;
        public static bool EqualsWildcardRegular(this string text, string wildcardString, bool ignoreCase)
        {
            Logger?.LogTrace($"{nameof(EqualsWildcardRegular)}({text}, {wildcardString}, {ignoreCase})");
            if (ignoreCase)
            {
                wildcardString = wildcardString.ToLower();
                text = text.ToLower();
            }
            var regularWildCard = "^" + System.Text.RegularExpressions.Regex.Escape(wildcardString).Replace("\\?", ".").Replace("\\*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(text, regularWildCard);
        }

        public static bool EqualsWildcard(this string text, string wildcardString, bool ignoreCase)
        {
            Logger?.LogTrace($"{nameof(EqualsWildcard)}({text}, {wildcardString}, {ignoreCase})");
            return ignoreCase
                ? text.ToLower().EqualsWildcard(wildcardString.ToLower())
                : text.EqualsWildcard(wildcardString);
        }

        public static bool EqualsWildcard(this string text, string wildcardString)
        {
            char[] word;
            char[] filter;
            var wi = 0;
            if (text.Length < wildcardString.Length)
            {
                //Text zu kurz
                Logger?.LogTrace("Text zu kurz");
                return false;
            }

            if (!wildcardString.Contains("*"))
            {
                Logger?.LogTrace("kein *");
                if (text.Length != wildcardString.Length)
                {
                    //Kein "*" => text muss gleich lang sein wie wildcardString
                    return false;
                }

                if (!wildcardString.Contains("?"))
                {
                    Logger?.LogTrace("kein * und kein ?");
                    return text == wildcardString;
                }

                word = text.ToCharArray();
                filter = wildcardString.ToCharArray();
                for (wi = 0; wi < word.Length; wi++)
                {
                    if (word[wi] != filter[wi] && filter[wi] != '?')
                    {
                        Logger?.LogTrace($"'{word[wi]}' != '{filter[wi]}' != '?' ");
                        return false;
                    }
                }
            }

            //alles beachten :-(
            word = text.ToCharArray();
            filter = wildcardString.ToCharArray();

            var fi = 0;
            var istWild = false;
            while (wi < word.Length && fi < filter.Length)
            {
                if (filter[fi] != word[wi] && filter[fi] != '?' && filter[fi] != '*' && !istWild)
                {
                    Logger?.LogTrace($"'{word[wi]}' != '{filter[wi]}' != '?' & !='*' && !istWild");
                    return false;
                }

                if (istWild && filter.Length - fi > word.Length - wi)
                {
                    Logger?.LogTrace($"istWild Restlänge {word.Length - wi} < Pflichtzeichen {filter.Length - fi}");
                    return false;
                }

                if (istWild && filter[fi] == word[wi])
                {
                    Logger?.LogTrace($"istWild Ende {wi}/{fi}");
                    istWild = false;
                }
                else if (filter[fi] == '*')
                {
                    Logger?.LogTrace($"istWild Start {wi}/{fi}");
                    istWild = true;
                    fi++;
                    if (filter.Length - fi <= 0)
                    {//abkürzung wenn letztes wildcardZeichen "*"
                        Logger?.LogTrace($"letztes wildcardZeichen ist '*'");
                        return true;
                    }
                }

                if (!istWild)
                {
                    fi++;
                }
                wi++;
            }

            Logger?.LogTrace($"{nameof(EqualsWildcard)}=>{istWild},{wi},{fi}");
            return wi >= word.Length;
        }
    }
}