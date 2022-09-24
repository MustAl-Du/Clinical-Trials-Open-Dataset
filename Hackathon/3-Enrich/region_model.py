import pandas as pd
import numpy as np
import re
import json

# Load in country->region mapping
map_file = open("region_mapping.json", 'r')
region_map = json.load(map_file)

# Returns a tuple with the region and confidence score
def assign_region(countries):
    if countries in region_map:
        return (region_map[countries], 1.0)
    else:
        # TO DO - Incorporate an NLP model to help classify countries that aren't in the mapping
        return "Unassigned"

# Example usage
def main():
    sample_df = pd.read_excel("Silver Samples 4712.xlsx", "Countries")
    sample_df['Region_Pred'] = sample_df.apply(lambda x: assign_region(x['Countries']), axis=1)
    print(sample_df.head())

if __name__ == "__main__":
    main()