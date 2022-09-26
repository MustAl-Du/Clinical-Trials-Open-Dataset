# Conversion of ClinicalTrials to FHIR (R4) ResearchStudy

## Overview
The ClinicalTrials to FHIR ResearchStudy conversion is part of a larger effort to transform Clinical Trial data from the 426,000+ clinical trials registered with [ClinicalTrials.gov](https://beta.clinicaltrials.gov/) into multiple queryable formats and persist these transformed data sets to appropriate data platforms. 

[FHIR](http://hl7.org/fhir/) (Fast Healthcare Interoperability Resources) is a data standard for health data that is being adopted rapidly by the healthcare industry. The FHIR data standard facilitates interoperability within health data, allowing researchers and clinicians to leverage clinical trials data through powerful queries, perform in-depth analyses, and draw powerful conclusions. 

At a high level, this project pulls Clinical Trials data from [ClinicalTrials.gov](https://beta.clinicaltrials.gov/) in JSON format, transforms each Clinical Trial to a FHIR [ResearchStudy](https://www.hl7.org/fhir/researchstudy.html) resource using a [Liquid](https://shopify.github.io/liquid/) template, and persists all ResearchStudy resources to [Microsoft's FHIR Service](https://learn.microsoft.com/en-us/azure/healthcare-apis/fhir/overview) (PaaS that allows users to securely store and exchange Protected Health Information in the cloud). This entire workflow can be implemented using [Azure Logic Apps](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-overview). Microsoft's FHIR Service currently supports the R4 version of FHIR, which is why Clinical Trials data is being converted to the FHIR R4 version of ResearchStudy, rather than the R5 version. 

## Work Done During Microsoft Hackathon 2022 (September)
During the Microsoft Hackathon from September 19-23, 2022, the following tasks were completed:

### 1. Created mapping between Clinical Trials data and FHIR (R4) ResearchStudy (unfinished)

| Field Name | FHIR Mapping | Mapping Details | 
|------------|--------------|-----------------|
| Primary Registry name | ResearchStudy.identifier.assigner.name | name of organization/registry |
| Trial Identifying Number | ResearchStudy.identifier.use | use = "official" |
| Trial Identifying Number | ResearchStudy.identifier.system | system = uri of namespace for the id value (primary registry - i.e. https://clinicaltrials.gov) |
| Trial Identifying Number | ResearchStudy.identifier.value | value = unique ID number (i.e. NCT05551637) |
| Date of Registration in Primary Registry | ResearchStudy.identifier.period.start | start = start date of time period when id is/was valid for use |
| Secondary Identifying Numbers (can have multiple) | ResearchStudy.identifier.use | use = "secondary" |
| Secondary Identifying Numbers (can have multiple) | ResearchStudy.identifier.value | value = identifier |
| Source(s) of Monetary or Material Support | ResearchStudy.annotation | text field. Only option, as R4 does not support funding source (R5 does) |
| Primary Sponsor | ResearchStudy.sponsor | |
| Secondary Sponsor(s) | ResearchStudy.annotation | text field. Only option, as R4 does not support funding source (R5 does) |
| Contact for Public Queries | ResearchStudy.contact.name | name = name of contact + specify that this is for public queries |
| Contact for Public Queries | ResearchStudy.contact.telecom | contact details (type is ContactPoint) |
| Contact for Scientific Queries | ResearchStudy.contact.name | name = name of contact + specify that this is for scientific queries |
| Contact for Scientific Queries | ResearchStudy.contact.telecom | contact details (type ContactPoint) |
| Public Title | ResearchStudy.title | |
| Scientific Title | ResearchStudy.identifier.use | use = "official" (CodeableConcept) |
| Scientific Title | ResearchStudy.identifier.type | type = CodeableConcept
| Scientific Title | ResearchStudy. Identifier.value | value = secondary title |
| Countries of Recruitment | ResearchStudy.enrollment.type | type = "person" | 
| Countries of Recruitment | ResearchStudy.enrollment.actual | actual = false (this is a descriptive group) |
| Countries of Recruitment | ResearchStudy.enrollment.quantity | quantity = number of people wanted that meet this criterion |
| Countries of Recruitment | ResearchStudy.enrollment.characteristics.code | code = CodeableConcept (GroupCharacteristicsKind) |
| Countries of Recruitment | ResearchStudy.enrollment.characteristics.value | value = value corresponding to criterion (i.e. country name as string) |
| Countries of Recruitment | ResearcgStudy.enrollment.characteristic.exclude | boolean, true = exclusion, false = inclusion |
| Health Condition(s) or Problem(s) Studied | ResearchStudy.condition | condition is a [CodeableConcept](https://hl7.org/fhir/2021may/valueset-condition-code.html) |
| Key Inclusion and Exclusion Criteria - attribute (i.e. age) | See inclusion/exclusion criteria format in "Countries of Recruitment" | |
| Key Inclusion and Exclusion Criteria - value (i.e. 30-50 years old) | See inclusion/exclusion criteria format in "Countries of Recruitment" | |
| Key Inclusion and Exclusion Criteria - included or excluded | See inclusion/exclusion criteria format in "Countries of Recruitment" | |

The above mapping was generated by inspecting the [specification](https://www.hl7.org/fhir/researchstudy.html) for the FHIR ResearchStudy resource and matching its data fields with corresponding fields in the JSON file representations of clinical trials downloaded from [ClinicalTrials.gov](https://beta.clinicaltrials.gov/). With limited time, fields that are mentioned in the [WHO Trial Registration Data Set specification](https://www.who.int/clinical-trials-registry-platform/network/who-data-set) were prioritized, though it is the hope that this mapping can be expanded to eventually include all appropriate attributes of the ResearchStudy resource. 

#### Additional WHO Trial Registration attributes whose fields were fleshed out, but not yet mapped:

|Attribute - Field |
|---------------------|
| Intervention(s) - Name |
| Intervention(s) - Description |
| Study type - type (interventional or observational) |
| Study type - design (can have multiple design aspects) |
| Study type - phase (if applicable) |
| Date of first enrollment (anticipated or actual) |
| Sample size - number of participants the trial plans to enrol |
| Sample size - number of participants the trial has enrolled |
| Recruitment status (pending, recruiting, suspended, complete, other) |
| Primary outcome(s) - name of outcome |
| Primary outcome(s) - metric/method of measurement |
| Primary outcome(s) - timepoint of primary interest |
| "Secondary outcome(s) - name of outcome |
| Secondary outcome(s) - metric/method of measurement |
| Secondary outcome(s) - timepoint of primary interest" |
| Ethics review - status (options: approved, not approved, not available) |
| Ethics review - date of approval |
| Ethics review - Ethics committee - name |
| Ethics review - Ethics committee -contact info |
| Completion date |
| Summary results - Date of posting of results summaries |
| Summary results - Date of the first journal publication of results |
| Summary results - URL hyperlink(s) related to results and publications |
| Summary results - Baseline Characteristics |
| Summary results - Participant flow |
| Summary results - Adverse events |
| Summary results - Outcome measures |
| Summary results - URL link to protocol file(s) with version and date |
| Summary results - Brief summary |
| IPD sharing statement - plan to share (options: yes, no) |
| IPD sharing statement - plan description |

### 2. Implemented a JSON to JSON Liquid template to facilitate conversion (unfinished)

The [conversion Liquid template](./ClinicalTrialToFhirR4/ConversionTemplates/JsonToFhirR4.liquid) includes mappings for a few key fields of the Clinical Trials data, and is meant to act as a starting point for further expansion of the mapping. FHIR resources can be uploaded to a FHIR server in JSON format, so this template maps the Clinical Trials JSON files to a FHIR ResearchStudy in JSON format. 

## Next Steps

### 1. Complete entire mapping of Clinical Trials data to FHIR (R4) ResearchStudy 
Continuation of work already done (see tables above).

### 2. Complete Liquid template implementation of mapping
Continuation of work done (see [conversion Liquid template](./ClinicalTrialToFhirR4/ConversionTemplates/JsonToFhirR4.liquid)).

### 3. Provision instance of FHIR Service

### 4. Configure 3-step workflow in Azure Logic Apps

## Useful Resources
- [Microsoft Open Source FHIR Converter](https://github.com/microsoft/FHIR-Converter/blob/main/README.md)
- [Liquid Templates in Logic Apps](https://www.c-sharpcorner.com/article/liquid-templates-in-logic-apps/)
- [FHIR service documentation](https://learn.microsoft.com/en-us/azure/healthcare-apis/fhir/)
- [Transform JSON and XML using Liquid templates as maps in workflows using Azure Logic Apps](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-enterprise-integration-liquid-transform?tabs=consumption)
- [Microsoft FHIR Converter](https://github.com/microsoft/FHIR-Converter)
