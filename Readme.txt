=========================
MBuild v1.29 - 2016/01/19
=========================
MBuild - A Marvelous Translation and Hacking Tool
=================================================
Written by DackR aka Daniel Martin Burgess
==========================================
!!!Requires .Net Framework 4.5
==============================
CREDITS:
Lunar Compress DLL v1.8 by FuSoYa.
Lunar Compress Interface Code Originally by giangurgolo and Omega (I've made some minor changes).
Super Bomberman 5 Decompression/Compression by Proton (Ported to C# by yours truely).
xDelta3 created by Joshua MacDonald (jmacd).
The SNES Checksum routine was ported to C# from the original SNES9x source.
=======================================================
ABOUT MBuild:
MBuild was originally created to assist in change management while
working on the translation of Marvelous: Another Treasure Island.
There are many features, with more features being added all the time.

Gone are the days of making permanant changes in a hex editor to the
original ROM file. With MBuild, the user is encouraged to extract data from
the original ROM and store it separately, in an organized way. When the time
comes to build the data, an XML file containing a list of data files, offsets,
and other information is used to neatly fold in the changes to the output file. 

Features:
Ability to dump binary data from a file.
Ability to dump script text.
Ability to encode script files into binary format.
Build-time compression using a variety of methods. 
Supports Lunar Compress DLL compression types.
Supports Super Bomberman 5 RLE-type compression.
Seperate command-line options for any process available during build-time.
Build-time SNES ROM file expansion support (pad option). 
Build-time SNES checksum correction and header modification (pad option).
Build-time diff patch creation supporting output of xdelta and/or ips files.
Drag-and-drop MBXML files on the MBuild executable to use that MBXML file to build a project.

Notes:
Relative paths are accepted.
PC hexadecimal offsets only.
Previous build files of the same name will be overwritten.
Modify the MBuild.MBXML (can be named differently) file to customize the build process.
XML Comments are supported.

MBXML file - XML Structure example:
<build original="ROMFILENAME.SFC" name="NameUsedToGenerateOutput" path="..\">
	<lzr file="x121964_LZR_TITLE.bin" offset="121964" />
	<lzr file="x1DA5ED_LZR_SUBTITLE.bin" offset="1DA5ED" />
	<lzr file="x1D948F_LZR_NINTENDO.bin" offset="1D948F" />
	<lzr file="x123951_LZR_INTROFONT.bin" offset="123951" />
	<lzr file="x1257F6_LZR_INTROTEXT.bin" offset="1257F6" />
	<lzr file="x14B628_LZR_PUZZLE1.bin" offset="14B628" />
	<lzr file="x132FC4_LZR_ANTBUSTER_GINARANSOM.bin" offset="132FC4" />
	
	<rep file="x270000_REP_KANJI_FONT.bin" offset="270000" />
	<rep file="x2AC000_REP_MAIN_FONT.bin" offset="2AC000" />
	<rep file="x2B6000_REP_ACTION_MENU_GFX.bin" offset="2B6000" />
	<rep file="x2CE000_REP_LARGE_FONT.bin" offset="2CE000" />
	<rep file="x7C84_REP_ASM_RELOCATE.bin" offset="7C84" />
	<rep file="x268000_REP_MISC_MENU_AND_NUMBERS.bin" offset="268000" />
	<rep file="x7FDC_REP_Possible_PalleteFix.bin" offset="7FDC" />
	
	<ins file="x300000_INS_EnglishScript.bin" offset="300000" />
</build>

Explanation/Notes:
To simplify the management of changes to a ROM file, 
you are able to keep separate binary files. This is for ease of backup and editing.

Supported Node Types:
build= 	attributes under this node control the basic build parameters such 
		as source file, name of output, and file path for all files.
rep=	Replace data at offset using RAW uncompressed data.
ins=	Insert data at offset using RAW uncompressed data. When inserting data, 
        the output file will be padded to the next valid ROM size by default.
lzr=	Stands for LZ, Replace. Essentially compresses binary data before
		replacing data at a specified offset with the compressed data.
		All Lunar Compress compression types are supported. 
lzi=	Stands for LZ, Insert. Compresses data and insert at specified offset.
bpr=	Bitplane conversion will be attempted. Currently, only two modes are supported.
bpi=	Same as above, but the data will be inserted into the outfile.
rlr=	RLE compression is performed before the data is overwritten at the specified offset.
rli=	Same as rlr, but the compressed data is inserted (file size changes).
sbr=	Script build and replace. When you specify a script text file and a table file,
		the data is build and then written to the output file at the specified offset.
sbi=	Same as sbr, but as an insert operation.

Additional Build Process Notes:
-Build process will follow node steps in order.
-Original ROM is not modified.
-Input files are not modified.
-Output file is generated fresh for each build.
-A unique file name is generated (for each date).XX- This is depreciated. Uses build info to generate the output file name.
-Multiple build nodes are supported, but nested builds are not.


Command-line arguments:

		"build"	-	Used to build a project using a specific XML file. 
				-	If no argument is specified, this process runs against "MBuild.XML" by default. 
				-	This behavior can be changed by modifying the "MBuild.exe.config" file's AutoBuild Setting.
				-	This argument only evaluates the /xmlfile property-- all others are ignored. 
				-	Example: MBuild build /xmlfile:"c:\folder\file.xml"

  "dump-script"	-	Used to dump an unformatted script file. (Currently) Support is planned for pointer table traversal.

   "bin-script" -	Used to build the script file (unicode/ascii txt file) into a binary-formatted file. Support is planned for Pointer table generation.

		"comp"  -	Uses one of any number of compression types to compress a file. Lunar Compress Types are supported as well as a few others.
				-	Example:

	   "fixsum" -	Use this argument to fix the checksum of SNES ROM files. Headered and Interleaved ROMs are unsupported.

		  "ips" -	Use this argument to generate an IPS patch.

	   "xdelta" -	Generates an xDelta3-compatible patch file.

  "bpp-convert" -	Convert a file between a few of the supported bpp formats.

	  "extract" -	Extract binary data from a file at a specific offset.
				-	Example 1: MBuild extract /input:"PathofFileToExtractFrom" /offset:"FFFF" /length:550
				-	Example 2: MBuild extract /input:"PathofFileToExtractFrom" /offset:FFFF /length:0x20 /output:"FilePathOfExtractedData"
				-	Example 3: MBuild extract /input:"PathofFileToExtractFrom" /offset:FFFF /endoffset:1200
				-	The /input and the /offset flags are required. The /length OR /endoffset flag is also required.
				-	The /output flag is optional and if unspecified, the outfile will be based on the input name and include the hexadecimal offset of the data that was extracted.
				-	Offsets can only be specified (currently) in hexadecimal. 
				-	The /length flag can be a hex number or a decimal number, but you must prefix a hex number with "x" or "0x".



  Working on several game-specific tools. Also general tools for bulk dumping of data utilizing pointer table data. (I need more examples of different pointer tables used in games.) So far, I am able to dump data utilizing a pointer table in the case of Marvelous and Super Bomberman 5.

X dmpdata

X decomp

X dumpptr