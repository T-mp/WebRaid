# WebRaid
## Idee
Die billigen (zum Teil kostenlosen) Angebote sind naturgemäß "unzuverlässig" (im Verhältnis zu teuer bezahlten) und auch in der Kapazität begrenzt.

"Früher" gabs doch mal die "**r**edundant **a**rray of **i**nexpensive **d**isks" kurz RAID (auch als "**r**edundant **a**rray of ***i**ndependent* **d**isks" bakannt ;-).  
Diesen Ansatz unabhängigen, billigen Speicher zusammen zu fassen, kann man auch auf Cloud-Speicher anwenden.

*Einfach* die „Disks“ durch Cloud-Storage tauschen und fertig. :innocent:

## Historie
Leider habe ich (auf die Schnelle) nix gefunden was dies umsetzt.

Da ich nicht nur Beruflich Software entwickle, habe ich dieses Projekt begonnen.  
Allerdings habe keine Lust auf Treiber schreiben und mir gefällt der Blockspeicher-Ansatz auch nicht.  
So bin ich auf die Idee gekommen das Ganze Dateibasiert auf zu ziehen und als Schnittstelle für die Programme / die Betriebssysteme WebDav zu verwenden.

## Ziele
- Beliebiges Programm greift auf Dateien / Ordner zu. Das Betriebssystem erfüllt diese Anfrage in dem es transparent, über WebDav und diesen Dienst auf mehrere Cloud-Speicher zugreift.
- Wenn einige (nicht zu fiele) Cloud-Speicher nicht verfügbar sind merkt das Programm nix.
- Cloud-Speicher können beliebig hinzugefügt werden und im Rahmen der genutzten Kapazitäten auch entfernt oder ausgetaucht werden. (Ohne nennenswerte Auswirkungen auf das verwendende Programm.)

## Rechtliches
Die Idee hinter RAID ist, glaube ich, schon alt genug, dass sie als Gemeingut in der IT durch geht.

Ob die Cloud-Speicher-Dienste diese Verwendung als „normale“ Nutzung betrachten, dulden oder verbieten ist im Einzelfall, vom Anwender, zu klären.

Die Techniken welche ich verwende und nicht von mir sind (z.B. WebDav, ReedSolomon) können geschützt sein!  
Hier ist jeder potentielle Anwender selber verantwortlich zu klären ob, diese in seinem rechtlichen Umfeld benutzt werden dürfen oder nicht!

Jegliche Verwendung / Nennung von Marken oder anderweitig geschützten Begriffen dient nur der Illustration, ich mache mir davon nichts zu Eigen.  
**Alle Rechte Dritter bleiben bei diesen!**

Im Zweifel dient das ganze Projekt nur der Information / Schulung der lesenden Personen.

**Das Ganze wird **ohne jegliche** Gewährleistung und nur zur Information zur Verfügung gestellt.**

Sollte ein Rechteinhaber der Meinung sein das ich hier seine Rechte missachte:  
Bitte melden, die Kosten für einen Anwalt können Sie sich sparen.  
Das Projekt ist nur ein privates Hobby, ich verfolge keinerlei finanziellen Interessen damit.

Sollte Jemand Interesse an meiner Schöpfung haben:
- Wer das Projekt privat und ohne finanzielle Interessen verwenden möchte:  
Darf dies ohne weitere Einschränkungen gerne machen (allerdings kann ich dies nur für meinen Anteil daran zusagen)  
Im Zweifel, gilt auch hier: Bitte melden.
- Für alle anderen: Ich verwehre mich nicht gegen die Verwendung, aber ich behalte mir das Recht vor hier im Einzelfall zu entscheiden. (Insbesondere sind hier vom potentiellen Verwender die rechtlichen Aspekte der grundlegenden Techniken Dritter zu klären!)

