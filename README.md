# MidiReverse
A simple console app that reverses MIDI files, i.e. makes files that are played backwards. Should work properly with Time Divisions, Tempo, Program, and Control changes.

Usage:

midireverse
  Reverses all files in the folder where MidiReverse.exe is. Output files will be created in \reverse folder. Requires confirmation by typing "Y" in the console.

midireverse -confirm
  Same as above, but doesn't require confirmation.

midireverse <filename> [output_folder]
    Reverse the specified MIDI file. Path to the file may be either relative or absolute. If no output folder is specified, output files will be created in \reverse folder.

midireverse <folder> [output_folder]
    Reverse all .mid files in the specified folder. If no output folder is specified, output files will be created in <input_folder>\reverse folder.
