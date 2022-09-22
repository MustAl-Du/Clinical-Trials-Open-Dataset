import csv
import json
import random
import codecs
import argparse
import os

parser = argparse.ArgumentParser(description='Generates importable files for Azure Cognitive Services')
parser.add_argument('-projectName', action="store", required=True)
parser.add_argument('-inputFile', action="store", required=True)
parser.add_argument('-storageContainer', action="store", required=True)
parser.add_argument('-description', action="store", default="Custom Entity Classifier")
parser.add_argument('-projectKind', action="store", default="CustomEntityRecognition")
parser.add_argument('-multilingual', action="store", default=False)
parser.add_argument('-lang', action="store", default="en")


args = parser.parse_args()

entity_types = set()
out_dir = "{}_train".format(args.projectName)
os.makedirs(out_dir)

with codecs.open(args.inputFile, encoding='utf-8') as f:
    csvFile = csv.reader(f, delimiter=',', quotechar='"')

    rowList = list(csvFile)
    
    documents = []
    for i in range(len(rowList)):
        filename = "entity_{}.txt".format(i)
        documents.append({})
        documents[i]["location"] = filename
        documents[i]["language"] = "en"
        documents[i]["dataset"] = "Train"

        entityLength = len(rowList[i][0])

        labels = {}
        labels["category"] = rowList[i][1]
        labels["offset"] = 0
        labels["length"] = entityLength
        entity_types.add(rowList[i][1])

        entities = {}
        entities["regionOffset"] = 0
        entities["regionLength"] = entityLength
        entities["labels"] = [labels]

        documents[i]["entities"] = [entities]
        with open(filename, "w", encoding="utf-8") as gen_file:
            gen_file.write(rowList[i][0])
        os.rename(filename, "./{}/{}".format(out_dir, filename))

    input_json = {}
    input_json["projectFileVersion"] = "2022-05-01"
    input_json["stringIndexType"] = "Utf16CodeUnit"

    metadata = {}
    metadata["projectKind"] = args.projectKind
    metadata["storageInputContainerName"] = args.projectKind
    metadata["projectName"] = args.projectName
    metadata["multilingual"] = args.multilingual
    metadata["description"] = args.description
    metadata["language"] = args.description
    input_json["metadata"] = metadata

    assets = {}
    assets["projectKind"] = args.projectKind
    assets["entities"] = []
    for category in entity_types:
        entity = {}
        entity["category"] = category
        assets["entities"].append(entity)

    assets["documents"] = documents
    input_json["assets"] = assets
    

    out_file = open("{}_format.json".format(args.projectName), "w")
    jsonString = json.dumps(input_json)
    out_file.write(jsonString)
    out_file.close()
