# ModelTextureMaker
*"Sorry, no punny quote for this one!"*

## Table of contents
- [Overview](#overview)
    - [Intended workflow](#intended-workflow)
- [How to use](#how-to-use)
    - [Basic usage](#basic-usage)
    - [Advanced options](#advanced-options)
    - [Texture-specific settings](#texture-specific-settings)
        - [modeltexturemaker.config files](#modeltexturemakerconfig-files)
        - [modeltexturemaker.config settings](#modeltexturemakerconfig-settings)
- [Texture types](#texture-types)
    - [Transparent textures](#transparent-textures)
    - [Color remap textures](#color-remap-textures)
    - [Other texture types](#other-texture-types)
- [Custom converters](#custom-converters)
    - [Using IrfanView for color conversion](#using-irfanview-for-color-conversion)
    - [Converting Gimp files](#converting-gimp-files)
    - [Converting Aseprite files](#converting-aseprite-files)
- [Credits](#credits)

## Overview
ModelTextureMaker is a command-line tool that converts directories full of images to 8-bit indexed bitmaps, which is the format required by Half-Life model compile tools. Existing texture directories can be updated quickly because only added, modified and removed images are processed. ModelTextureMaker can also extract textures from models, and replace textures in model files without having to recompile them.

ModelTextureMaker accepts the following image file formats:
- Png, jpg, gif, bmp and tga files.
- Photoshop files (.psd, .psb) that have been saved with 'Maximize compatibility' enabled.
- Krita files (.kra, .ora).
- Paint.NET files (.pdn).
- Other formats can be used with the help of external conversion tools.

It will automatically create a suitable 256-color palette for each texture, taking transparency and color remapping into account. By default it also applies a limited form of dithering. For textures with alpha-test transparency, ModelTextureMaker expects input images with transparency, but it can also be configured to accept images where transparent parts are marked with a special color. All these settings can be specified in a plain-text modeltexturemaker.config file in the images directory. Some settings, such as the color remap range, can also be set with input image filenames.

### Intended workflow
Existing workflows sometimes involve a lot of steps, such as exporting or converting images to 8-bit indexed bitmaps, manually creating and merging palettes, marking transparent areas with special colors, opening a GUI tool, dragging images into it, then saving the modified textures, and so on.

ModelTextureMaker aims to simplify this. Dragging a directory onto ModelTextureMaker or running a single batch file should be enough to convert all images in a directory to textures. No exporting or converting, no palette adjustments, no clicking around in a GUI tool. Just modify some images, run a batch file, and go.

## How to use
### Basic usage
For basic usage, directories and files can be dragged onto `ModelTextureMaker.exe`:
- To **make 8-bit indexed bitmaps**, drag the directory that contains your images onto `ModelTextureMaker.exe`. Textures will be put in a 'directoryname_textures' folder next to the input folder. If the output folder already exists, then only added, modified and removed images will be processed.
- To **extract textures from a model**, drag a model onto `ModelTextureMaker.exe`. The exported images will be put in a 'modelname_extracted' directory next to the model file.
- To **replace textures in a model**, open a command-line or create a batch file, and call ModelTextureMaker as following: `ModelTextureMaker.exe "C:\your\images\directory" "C:\your\model\file.mdl"`.

### Advanced options
The behavior of ModelTextureMaker can be modified with several command-line options. To use these, you will have to call ModelTextureMaker from a command-line or from a batch file. The following options are available (options must be put before the input directory or file path):
- **-subdirs** - Makes ModelTextureMaker also process sub-directories, creating a matching output folder hierarchy.
- **-full** - Forces ModelTextureMaker to rebuild all textures, instead of processing only added, modified and deleted images.
- **-subdirremoval** - Enables deleting of output sub-directories, when input sub-directories are removed. Be careful with this option if your output sub-directories also contain bitmaps that were not created by ModelTextureMaker - those will also be removed.
- **-overwrite** - Enables overwriting of existing image files when extracting textures from models.
- **-format: \<fmt\>** - Extracted images output format (\<fmt\> must be `png`, `jpg`, `gif`, `bmp` or `tga`).
- **-indexed** - Extract textures as 8-bit indexed images (only works with png and bmp).
- **-nologfile** - Prevents ModelTextureMaker from creating log files.

It is also possible to specify a custom output location when making textures. For example:
`"C:\HL\tools\ModelTextureMaker.exe" -subdirs -subdirremoval "C:\HL\mymod\models\textures\source" "C:\HL\mymod\models\textures\8bit"` will take all images in `C:\HL\mymod\models\textures\source` and its sub-directories, and use them to create, update or remove textures in `C:\HL\mymod\models\textures\8bit`.

The same can be done when extracting textures from a model. For example:
`"C:\HL\tools\ModelTextureMaker.exe" -overwrite "C:\Program Files (x86)\Steam\steamapps\common\Half-Life\mymod\models\tree.mdl" "C:\HL\extracted\models\tree"` will save all extracted textures in `C:\HL\extracted\models\tree`, overwriting any existing files in that directory.

This also works when replacing textures in a model. For example: `"C:\HL\tools\ModelTextureMaker.exe" "C:\HL\mymod\models\textures\source" "C:\Program Files (x86)\Steam\steamapps\common\Half-Life\mymod\models\tree.mdl" "C:\Program Files (x86)\Steam\steamapps\common\Half-Life\mymod\models\tree_v2.mdl"` will save the model with updated textures as `tree_v2.mdl`, instead of modifying the `tree.mdl` file.

### Texture-specific settings
#### Filename settings

A few settings, specifically for color remap textures, can be specified in the filename of an image. Filename settings are separated by dots, the part before the first dot becomes the output texture filename. For example, `dm_base.png`, `dm_base.color1.png` and `dm_base.color2.png` together produce a single `dm_base.bmp` texture, with the color1 and color2 images used for specific remappable color ranges. For more information, see [color remap textures](#color-remap-textures).

#### modeltexturemaker.config files

Less common settings can be specified per texture, or per group of textures, by creating a plain-text `modeltexturemaker.config` file in the images directory. For global settings, use the `modeltexturemaker.config` file in ModelTextureMaker.exe's directory. Global rules are overridden by local rules with the same name.

A settings line starts with a texture name or a name pattern, followed by one or more settings. Empty lines and comments are ignored. For example:

    // This is a comment. The next lines contain texture settings:
    *            dither-scale: 0.5
    *.at         transparency-color: 0 0 255
    tree         dithering: none
    *.xcf        converter: '"C:\Tools\XcfToPngConverter.exe"'       arguments: '/in="{input}" /out="{output}"'
This sets the dither-scale to 0.5 for all textures, and it tells ModelTextureMaker to treat blue (0 0 255) as transparent for all images whose filename contains '.at'. It also disables dithering for the 'tree' image. Finally, it tells ModelTextureMaker to call a converter application for each .xcf file in the image directory - ModelTextureMaker will then use the output image(s) produced by that application.

If there are multiple matching rules, all of their settings will be applied in order of appearance. In the above example, a texture named `tree` will use a dither-scale of 0.5 (because of the `*` rule) but dithering will also be disabled for it (because of the `tree` rule). If the `tree` rule would also have specified a dither-scale, then that dither-scale would have been used instead, because the `tree` rule comes after the `*` rule.

ModelTextureMaker keeps track of settings history in a `modeltexturemaker.dat` file. This enables it to only update textures whose settings have been modified (if `-full` mode is not enabled).

#### modeltexturemaker.config settings

Texture settings:

- **color-mask: color-mask** - Color mask must be `main`, `color1` or `color2`. This determines which remap color range the image is used for. The default is 'main'.
- **color-count: count** - The number of colors that will be reserved in the palette for this color-mask. This only applies to remapX textures. For the 'main' color mask, this determines the offset of the color1 palette range.
- **is-model-portrait: true/false** - When true, the image (and associated `.color1` and `.color2` images) is converted to a model portrait bitmap, using the same approach as dm_base textures, except that color1 and color2 will be swapped.
- **ignore: true/false** - When true, matching files will be ignored. This can be used to exclude certain files or file types from the input directory.
- **preserve-palette: true/false** - When true, input images that are already in an 8-bit indexed format will not be quantized - their palette will be used as-is. No special texture-type specific handling will be performed.

Dithering:

- **dithering: type** - Type must be either `none` or `floyd-steinberg`. The default is Floyd-Steinberg dithering.
- **dither-scale: scale** - Scale must be a value between 0 (disables dithering) and 1 (full error  diffusion). The default is 0.75, which softens the effect somewhat.

Alpha-test settings:

- **transparency-threshold: threshold** - Threshold must be a value between 0 and 255. The default is 128. Any pixel whose alpha value is below this threshold will be marked as  transparent.
- **transparency-color: r g b** - A color, written as 3 whitespace-separated numbers, with each number between 0 and 255. Pixels with this color will be marked as transparent.

Conversion settings:

- **converter: 'path'** - The path of an application that can convert a file into one or more image files. If the path contains spaces then it should be surrounded by double quotes. The whole path, including any double quotes, must be delimited by single quotes. Any single quotes in the path itself must be escaped with a `\`. For example, the path `C:\what's that.exe` should be written as `'"C:\what\'s that.exe"'`.
- **arguments: 'arguments'** - The arguments that will be passed to the converter application, surrounded by single quotes. The arguments must contain an input and output placeholder (see below). As with the converter setting, the whole arguments list must be delimited by single quotes, and any path that contains spaces should be surrounded by double quotes. The following placeholders can be used:
  - `{input}` - The full path of the file that will be converted, for example: `C:\HL\mymod\models\textures\source\treebark.ase.`
  - `{input_escaped}` - Same as `{input}`, but with escaped backslashes: `C:\\HL\\mymod\\models\\textures\\source\\treebark.ase`.
  - `{output}` - The full path of where ModelTextureMaker expects to find the output file(s), without extension. For example: `C:\HL\mymod\models\textures\source\converted_12345678-9abc-def0-1234-56789abcdef0\treebark`.
  - `{output_escaped}` - Same as `{output}`, but with escaped backslashes: `C:\\HL\\mymod\\models\\textures\\source\\converted_12345678-9abc-def0-1234-56789abcdef0\\treebark`.

## Texture types
Half-Life model textures use a 256-color palette. Texture names cannot be longer than 64 characters (this includes the `.bmp` file extension).

Note that textures do not store color profile information, and because Half-Life does not appear to apply gamma correction properly on all systems, textures (especially dark ones) may look too bright on some systems.

### Transparent textures
Transparent textures require two things:
- They must be marked as such when compiling a model. This is done by adding a line like `$texrendermode texture.bmp masked` to the .qc file.
- Transparent parts must use the last color in the palette (#255).

ModelTextureMaker will automatically detect whether an input image contains transparency, and it will use the last palette color for transparent parts, so all that remains is the .qc file part.

When updating textures in an existing model, ModelTextureMaker will automatically enable or disable transparency for a texture, depending on whether the input image has transparent parts.

### Color remap textures
Some textures contain parts with changeable colors. This is used specifically for multiplayer models, but it can also be used for other models. Color remap textures consist of 3 parts:
- Normal (main) - the color of these pixels always remains the same.
- Top color (color1) - the hue of these pixels is controlled by the game.
- Bottom color (color2) - the hue of these pixels is also controlled by the game.

By default, color1 and color2 each get 32 colors in the palette. The remaining 192 colors are used for normal pixels. Color remap textures cannot contain transparent parts.

There are 3 kinds of color remap textures. In all cases, input images should use the `texturename.color1.png` and `texturename.color2.png` filename convention for the top and bottom color parts. These images should be transparent, except for the areas with remappable colors.

**Model portraits**
Multiplayer models come with a 164x200 image that has remappable colors. These work in the same way as `dm_base` textures, except that the top and bottom color are swapped. ModelTextureMaker accounts for that by automatically swapping them for you.

Model portrait images use the following naming convention:
- `modelname.portrait.png` - The `.portrait` part tells ModelTextureMaker this this is a model portrait image.
- `modelname.color1.png` - The top color parts of the model portrait.
- `modelname.color2.png` - The bottom color parts of the model portrait.

**dm_base textures**
If a texture has the special name `dm_base`, then parts of its palette will have their color (hue) controlled by the game.

`db_base` textures use the following naming convention:
- `dm_base.png` - The name `dm_base` is special, so ModelTextureMaker will automatically treat this as a color remap texture.
- `dm_base.color1.png` - The top color parts of this texture.
- `dm_base.color2.png` - The bottom color parts of this texture.

**remapX textures**
This filename convention is a newer mechanism that enables the use of multiple color remap textures in a single model. Besides that, the main difference with `dm_base` textures is that the number of palette colors is configurable for each part.

`remapX` textures use the following naming convention:
- `remapX.png` - The name `remap`, followed by a single digit or character, is special, so ModelTextureMaker will automatically treat this as a color remap texture.
- `remapX.color1.png` - The top color parts of this texture.
- `remapX.color2.png` - The bottom color parts of this texture.

To allocate different number of colors for specific parts, add a count after the color1/color2 part:
- `remapX.color1 128.png` - The top color parts of the texture now use 128 colors in the palette.
- `remapX.color2 16.png` - The bottom color parts of the texture now use only 16 colors in the palette.
The main part of the texture will now use 112 colors, because 256 - 128 - 16 = 112.

The output file will be something like `remapX_112_239_255.bmp`. The game uses these numbers to determine which parts of the palette are affected by color remapping. ModelTextureMaker automatically determines these numbers based on the specified color counts.

### Other texture types
There are other texture types, such as chrome, additive, flat shaded and fullbright, but these do not require special processing.

## Custom converters
ModelTextureMaker can be configured to use custom converters for certain images. This makes it possible to achieve better visual results, or to handle file types that ModelTextureMaker does not support directly. IrfanView is particularly useful in this regard, but any other command-line program can be used, as long as both the input and output path can be provided as arguments. It's a good idea to put conversion rules in the global `modeltexturemaker.config` file, so they don't need to be repeated in every directory's `modeltexturemaker.config` file.

### Using IrfanView for color conversion
To use IrfanView to convert images to 256 colors, add the following line to your `modeltexturemaker.config` file:

    texturename      converter: '"C:\Program Files\IrfanView\i_view64.exe"' arguments: '"{input}" /silent /bpp=8 /convert="{output}.png"'
Or, when using advanced batch settings, save the right IrfanView batch settings to an `i_view64.ini` file, and specify the directory in which that ini file is located:

    texturename      converter: '"C:\Program Files\IrfanView\i_view64.exe"' arguments: '"{input}" /silent /ini="C:\custom_irfanview_settings_dir" /advancedbatch /convert="{output}.png"'
To use this conversion for multiple images, replace `texturename` with a wildcard pattern such as `if_*`, so any images whose name starts with `if_` will be converted using IrfanView.

### Using pngquant for color conversion
To use pngquant to convert images to 256 colors, add the following line to your `modeltexturemaker.config` file:

    texturename     converter: '"C:\HL\tools\pngquant\pngquant.exe"'    arguments: '"{input}" --output "{output}.png"'

### Converting Gimp files
To automatically convert Gimp files, add the following line to your `modeltexturemaker.config` file:

    *.xcf       converter: '"C:\Program Files\GIMP 2\bin\gimp-console-2.10.exe"' arguments: '-nidc -b "(let* ((image (car (gimp-file-load RUN-NONINTERACTIVE """{input_escaped}""" """{input_escaped}"""))) (layer (car (gimp-image-merge-visible-layers image CLIP-TO-IMAGE)))) (gimp-file-save RUN-NONINTERACTIVE image layer """{output_escaped}.png""" """{output_escaped}.png""") (gimp-image-delete image) (gimp-quit 1))"'

This uses Gimp's command-line Script-Fu batch interpreter to open the specified image, merge all its visible layers and save the result to the conversion output location. ModelTextureMaker then reads the resulting png file and uses it to create a texture.

### Converting Aseprite files
To automatically convert Aseprite files, add the following lines to your `modeltexturemaker.config` file:

    *.ase           converter: '"C:\Applications\Aseprite\aseprite.exe"' arguments: '-b "{input}" --save-as "{output}.png"'
    *.aseprite      converter: '"C:\Applications\Aseprite\aseprite.exe"' arguments: '-b "{input}" --save-as "{output}.png"'

The `-b` switch prevents Aseprite from starting its UI. ModelTextureMaker then reads the resulting png file and uses it to create a texture. See [Aseprite Command Line Interface ](https://www.aseprite.org/docs/cli/) for more information about using Aseprite from the command-line.

## Credits
- Thanks to [malortie](https://github.com/malortie) for documenting the Half-Life model file format.
- Thanks to [The303](http://www.the303.org/) for his information about masked textures and color remap textures.
- ModelTextureMaker uses the [ImageSharp](https://github.com/SixLabors/ImageSharp) library, which is licensed under the Apache License 2.0.