
The file "SLOT_A_INSTALLER.WAV" (or .tap) is intended to be used to 
re-install the SMART card's firmware (IE: the ROM manager) that
resides in ROM slot A should it ever become corrupt. 

***************************************************************
DO NOT USE THIS PROCEDURE FOR NORMAL FIRMWARE *UPDATES* 
(ie: when the card is working correctly but you want to update
the firmware - for that use the ROM Manager itself (see the
folder "Firmware Updates" for details). The reason being
is this will wipe the index of ROM contents (though the data
in slots B-P will actually remain intact.)
***************************************************************

------------------------------------------------------------------------

SMART Card V1 only: With the power off, set the DIP switches as follows:

Sw1 - OFF (down)
Sw2 - ON  (up)
Sw3 - OFF (down)
Sw4 - Doesn't matter

------------------------------------------------------------------------

1. Put the 16K ROM file FIRMWARE.Vxx from this folder onto your SD Card
   (and the ROMs from the ROM folder if you want to re-install those too).

2. Connect the Spectrum EAR socket to a device that is able to
   play .wav files loud enough for the Spectrum to pick up. 

3. Put the SD Card in the SMART Card. 

4. Power up the Spectrum. (For V2 SMART Card only: Hold the SMART Card's
   reset (left) button for 2 seconds whilst powering on the Spectrum, then
   release it - the Spectrum should boot to BASIC.)

5. Enter the BASIC command LOAD "" [ENTER]

6. Play the file "slot_a_installer.wav" from this folder (You can use the
   .tap version of this .wav file with a PC app such as TAPIR instead if 
   you wish) 

7. Follow the on-screen instructions when the progam has loaded. (Basically, 
   you’ll browse to and select the FIRMWARE.Vxx file you placed on the SD
   Card earlier. The border will flash during re-programming.) 

8. Upon completion, power off


At this point, the card will boot to a ROM list / selector.  As the
procedure above will have wiped the ROM slot index (but not the actual
data in slots B-P) you should be able to just rename the slots now marked
as "Empty", but it's probably safer to re-install them as follows:

9.  On boot, press Enter to go to the ROM Manager.

10. Re-install the ROMs using option [2]. The DiagROM should go in slot B
    (so that it is correctly activated with the switch on the back of the PCB).
    SNAPLOAD and the other ROMs can go in any slot.

11. Set the power-on GOTO bank to that which holds SNAPLOAD with option 6.


-------------------------------------------------------------------------

For SMART Card V1 only: Put the DIP switches back to normal use:

Sw1 - ON  (up)
Sw2 - as desired, but preferably OFF (down) to prevent accidental writes.
Sw3 - OFF (down)
Sw4 - as desired, usually ON (to enable the Kempston joystick port)

--------------------------------------------------------------------------


12. Power off / power on.




