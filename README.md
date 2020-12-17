# WebRaid
## Idee
Die billigen (zum Teil kostenlosen) Angebote sind naturgem�� "unzuverl�ssig" (im Verh�ltnis zu teuer bezahlten) und auch in der Kapazit�t begrenzt.

"Fr�her" gabs doch mal die "**r**edundant **a**rray of **i**nexpensive **d**isks" kurz RAID (auch als "**r**edundant **a**rray of ***i**ndependent* **d**isks" bakannt ;-).  
Diesen Ansatz unabh�ngigen, billigen Speicher zusammen zu fassen, kann man auch auf Cloud-Speicher anwenden.

*Einfach* die �Dicks� durch Cloud-Storage tauschen und fertig. :innocent:

## Historie
Leider habe ich (auf die Schnelle) nix gefunden was dies umsetzt.

Da ich nicht nur Beruflich Software entwickle, habe ich dieses Projekt begonnen.  
Allerdings habe keine Lust auf Treiber schreiben und mir gef�llt der Blockspeicher-Ansatz auch nicht.  
So bin ich auf die Idee gekommen das Ganze Dateibasiert auf zu ziehen und als Schnittstelle f�r die Programme / die Betriebssysteme WebDav zu verwenden.

## Ziele
- Beliebiges Programm greift auf Dateien / Ordner zu. Das Betriebssystem erf�llt diese Anfrage in dem es transparent auf mehrere Cloud-Speicher zugreift.
- Wenn einige (nicht zu fiele) Cloud-Speicher nicht verf�gbar sind merkt das Programm nix.
- Cloud-Speicher k�nnen beliebig hinzugef�gt werden und im Rahmen der genutzten Kapazit�ten auch entfernt oder ausgetaucht werden. (Ohne nennenswerte Auswirkungen auf das verwendende Programm.)

## Rechtliches
Die Idee hinter RAID ist, glaube ich, schon alt genug, dass sie als Gemeingut in der IT durch geht.

Ob die Cloud-Speicher-Dienste diese Verwendung als �normale� Nutzung betrachten, dulden oder verbieten ist im Einzelfall, vom Anwender, zu kl�ren.

Die Techniken welche ich verwende und nicht von mir sind (z.B. WebDav, ReedSolomon) k�nnen gesch�tzt sein!  
Hier ist jeder potentielle Anwender selber verantwortlich zu kl�ren ob sie in seinem rechtlichen Umfeld diese benutzt werden d�rfen oder nicht!

Jegliche Verwendung / Nennung von Marken oder anderweitig gesch�tzten Begriffen dient nur der Illustration, ich mache mir davon nichts zu Eigen.  
**Alle Rechte Dritter bleiben bei diesen!**

Im Zweifel dient das ganze Projekt nur der Information / Schulung der lesenden Personen.

**Das Ganze wird **ohne jegliche** Gew�hrleistung und nur zur Information zur Verf�gung gestellt.**

Sollte ein Rechteinhaber der Meinung sein das ich hier seine Rechte missachte:  
Bitte melden, die Kosten f�r einen Anwalt kann man sich sicherlich sparen.  
Das Projekt ist nur ein privates Hobby, ich verfolge keinerlei finanziellen Interessen damit.

Sollte Jemand Interesse an meiner Sch�pfung haben:
- Wer das Projekt privat und ohne finanzielle Interessen verwenden m�chte:  
Darf dies ohne weitere Einschr�nkungen gerne machen (allerdings kann ich dies nur f�r meinen Anteil daran zusagen)  
Im Zweifel, gilt auch hier: Bitte melden.
- F�r alle anderen: Ich verwehre mich nicht gegen die Verwendung, aber ich behalte mir das Recht vor hier im Einzelfall zu entscheiden. (Insbesondere sind hier vom potentiellen Verwender die rechtlichen Aspekte der grundlegenden Techniken Dritter zu kl�ren!)

