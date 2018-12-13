# download-all-videos-from-m3u8-file
We got a m3u8 file and want to get all the videos but you're lazy man that don't want to do it manually. This tool is what you need

This build by .netcore 2.1, only run in windows.

After build, copy you m3u8 file to debug folder, open cmd and run `dotnet DownloadBathFile.dll`

------------------------------------------

If don't know how to get the m3u8 file, please read this thread: https://stackoverflow.com/questions/42901942/how-do-we-download-a-blob-url-video
 
------------------------------------------

Want to concat files? using FFMPEG:

download ffmpeg for windows: https://ffmpeg.zeranoe.com/builds/

Automatically generating the input file:
run cmd command `(for %i in (*.mp4) do @echo file '%i') > mylist.txt`

then `ffmpeg -f concat -safe 0 -i mylist.txt -c copy output.mp4`

read more about comcat command: https://trac.ffmpeg.org/wiki/Concatenate
