# How to use:
- install python 3
- pip any required packages
- run with an input file with a csv format of `entity,category` (and no category header)
- install azcopy
- run the upload command

### Usage Example
- run `.\generate_input.py -storageContainer "test-condition" -projectName "AutoCondition" -inputFile conditions_raw.txt`
- make sure you have an existing storage container named according to your args
- login to azcopy (in elevated powershell) with `azcopy login`
- run `azcopy copy 'C:\Clinical-Trials-Open-Dataset\Classification\AutoCondition_train\*' 'https://kdhhackathonstorage.blob.core.windows.net/test-condition'`
- upload the autogenerated format json to Azure Cognitive Services at `https://language.cognitive.azure.com/customText/projects/extraction`

The files used in the example above can be found in this directory.

### Gotchas
- project name formatting is restrictive. Stick to alphanumeric values without spaces if you're encountering issues
- entity categories are limited to 50 characters in length. You may need to rename some categories
  - ex: `Surgery, Abdominal Disease, Emergency and Intensive Care` -> `Surgery, Abdominal Disease, Emergency and ICU`
- multilanguage support is not implemented by this script