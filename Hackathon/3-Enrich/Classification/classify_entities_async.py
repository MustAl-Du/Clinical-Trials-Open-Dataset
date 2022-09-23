# -------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
# --------------------------------------------------------------------------

"""
SOURCE: https://github.com/Azure/azure-sdk-for-python/blob/azure-ai-textanalytics_5.2.0b4/sdk/textanalytics/azure-ai-textanalytics/samples/async_samples/sample_recognize_custom_entities_async.py
DESCRIPTION:
    This sample demonstrates how to recognize custom entities in documents.
    Recognizing custom entities is also available as an action type through the begin_analyze_actions API.
    For information on regional support of custom features and how to train a model to
    recognize custom entities, see https://aka.ms/azsdk/textanalytics/customentityrecognition
USAGE:
    python sample_recognize_custom_entities_async.py
    Set the environment variables with your own values before running the sample:
    1) AZURE_LANGUAGE_ENDPOINT - the endpoint to your Language resource.
    2) AZURE_LANGUAGE_KEY - your Language subscription key
    3) CUSTOM_ENTITIES_PROJECT_NAME - your Language Language Studio project name
    4) CUSTOM_ENTITIES_DEPLOYMENT_NAME - your Language deployed model name
"""


import os
import asyncio
import csv
import codecs
from time import perf_counter


async def recognize_custom_entities_async() -> None:
    # [START recognize_custom_entities_async]
    from azure.core.credentials import AzureKeyCredential
    from azure.ai.textanalytics.aio import TextAnalyticsClient

    env = {}
    with open("keys.txt") as keys:
        env_vars = csv.reader(keys)
        for item in env_vars:
            env[item[0]] = item[1]
    path_to_input_doc = os.path.abspath(
        os.path.join(
            os.path.abspath(__file__),
            "..",
            "./{}".format(env["input_file"]),
        )
    )

    text_analytics_client_1 = TextAnalyticsClient(
        endpoint=env["endpoint"],
        credential=AzureKeyCredential(env["key"]),
    )
    text_analytics_client_2 = TextAnalyticsClient(
        endpoint=env["endpoint"],
        credential=AzureKeyCredential(env["key"]),
    )

    fd = codecs.open(path_to_input_doc, encoding='utf-8')
    reader = csv.reader(fd, delimiter=',', quotechar='"')

    header = next(reader)
    condition_idx = header.index("condition")
    funding_idx = header.index("source")

    timer_start = perf_counter()

    input_csv = list(reader)
    conditions_classified = await classify(
        env["condition_project"], env["condition_deployment"], input_csv, condition_idx, text_analytics_client_1
        )
    funding_classified = await classify(
        env["funding_project"], env["funding_deployment"], input_csv, funding_idx, text_analytics_client_2
        )

    timer_stop = perf_counter()
    print("Elapsed time to classify (s): ", timer_stop-timer_start)

    fd.close()

    fd = codecs.open(path_to_input_doc, encoding='utf-8')
    reader = csv.reader(fd, delimiter=',', quotechar='"')

    outFile = codecs.open('enriched.csv', 'w', encoding='utf-8')
    writer = csv.writer(outFile)

    header = next(reader)
    header[funding_idx+1:funding_idx+1] = ["predicted_funding_source", "funding_confidence"]
    header[condition_idx+1:condition_idx+1] = ["predicted_condition", "condition_confidence"]

    writer.writerow(header)

    outCsv = list(reader)
    for i in range(len(outCsv)):
        outCsv[i][funding_idx+1:funding_idx+1] = funding_classified[i]
        outCsv[i][condition_idx+1:condition_idx+1] = conditions_classified[i]
        writer.writerow(outCsv[i])

    fd.close()
    outFile.close()

    # [END recognize_custom_entities_async]


async def classify(project, deployment, source, index, client):
    outList = []
    input_docs = list(map(lambda item: item[index].replace('"', '').replace('[', '').replace(']', ''), source))
    
    input_docs_split = list(split_list(input_docs, 25))

    poller_list = []
    async with client:
        for section in input_docs_split:
            poller_list.append(await client.begin_recognize_custom_entities(
                section,
                project_name=project,
                deployment_name=deployment
            ))

        for poller in poller_list:
            document_results = await poller.result()
            async for custom_entities_result in document_results:
                if custom_entities_result.kind == "CustomEntityRecognition":
                    best_match = None
                    best_confidence = 0
                    for entity in custom_entities_result.entities:
                        if (entity.confidence_score > best_confidence):
                            best_match = entity
                    outList.append([best_match.category, best_match.confidence_score])
                elif custom_entities_result.is_error is True:
                    print("...Is an error with code '{}' and message '{}'".format(
                        custom_entities_result.code, custom_entities_result.message
                        )
                    )

    return outList

def split_list(l, split_size):
    for i in range(0, len(l), split_size):
        yield l[i:i+split_size]

async def main():
    await recognize_custom_entities_async()


if __name__ == '__main__':
    asyncio.run(main())