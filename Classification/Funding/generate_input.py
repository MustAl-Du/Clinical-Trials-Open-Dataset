import csv
import json
import random
import unidecode
import unicodedata
import codecs


def getRowLength(row):
    return len(row[0]) + len(row[1]) + 3

# def getCharLength(item):
#     try:
#         return len(item.encode('latin-1', errors='ignore').decode('utf-8'))
#     except:
#         return len(item.encode('latin-1', errors='ignore'))

with codecs.open('silver_samples_raw.txt', encoding='utf-8') as f:
    csvFile = csv.reader(f, delimiter=',', quotechar='"')


    rowList = list(csvFile)
    # print(rowList[0][0])
    regionOffset = getRowLength(rowList[0])
    offset = 0
    # print(regionOffset)
    
    documents = []
    for i in range(len(rowList)):
        filename = "entity_{}.txt".format(i)
        documents.append({})
        documents[i]["location"] = filename
        documents[i]["language"] = "en"
        documents[i]["dataset"] = "Train"

        entityLength = len(rowList[i][0])
        if i == 1300 or i == 144 or i==3115:
            print(entityLength)
            # test = rowList[i][0].encode('latin-1', errors='ignore')
            # print(test)
            # print(len(test))
            # print(rowList[i][0])
            # print(len(rowList[i][0]))

        labels = {}
        labels["category"] = rowList[i][1]
        labels["offset"] = 0
        labels["length"] = entityLength

        entities = {}
        entities["regionOffset"] = 0
        entities["regionLength"] = entityLength
        entities["labels"] = [labels]

        documents[i]["entities"] = [entities]
        with open(filename, "w", encoding="utf-8") as gen_file:
            gen_file.write(rowList[i][0])

    # print(labels)
    regionLength = offset
    # print(regionOffset)

    out_file = open("out.json", "w")
    jsonString = json.dumps(documents)
    out_file.write(jsonString)


    # for lines in csvFile:
    #     print(lines)