REM This is an example batch file that uses ModelTextureMaker to convert all images in 'C:\HL\mymod\models\textures\source' and its sub-directories
REM into 8-bit indexed bitmaps in the 'C:\HL\mymod\models\textures\8bit' directory.
REM The -subdirs option causes ModelTextureMaker to also process sub-directories, creating a matching directory hierarchy in the output directory.
REM The -subdirremoval option causes ModelTextureMaker to remove output sub-directories if the corresponding input sub-directory is removed.

REM To use this batch file, replace the directory paths below with the right paths for your system, and then remove the 'REM ' from the line below:
REM "C:\HL\tools\ModelTextureMaker.exe" -subdirs -subdirremoval "C:\HL\mymod\models\textures\source" "C:\HL\mymod\models\textures\8bit"

PAUSE