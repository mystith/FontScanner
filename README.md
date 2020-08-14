# FontScanner

This program was designed for scanning bitmap text (non anti-aliased) and producing images of each character.
Intended to be used with another program, Goldies-OCR, for scanning an image for characters of a bitmap font.
Each character scanned must be manually labeled (a scan of the letter 'A' must be labelled manually as an 'A'.

#Usage
Image inputted must be fairly clear.

To run, run application via command prompt or terminal with an argument linking the directory containing the image that would be scanned.
Optional argument "boolean" will change output to text files of 1s and 0s, a 1 indicating a while pixel and a 0 indicating black.
