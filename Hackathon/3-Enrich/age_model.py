from lib2to3.pytree import convert
import pandas as pd
import numpy as np
import re

def convert_to_years(age_string):
    # Get digit out
    digits = re.findall("\d*", age_string)[0]

    if "Year" in age_string:
        return float(digits)
    if "Month" in age_string:
        return float(digits) / 12
    if "Week" in age_string:
        return float(digits) / 52
    if "Day" in age_string:
        return float(digits) / 365
    if "Hour" in age_string:
        return float(digits) / (365 * 24)
    elif digits != '': # If no unit is provided, assume years
        return float(digits)
    else:
        return np.nan

def assign_age_group(min_age, max_age):
    # Step 1 - Convert strings to age in years
    min_age = convert_to_years(min_age)
    max_age = convert_to_years(max_age)

    # Step 2 - Apply age group rules
    category_string = ''

    if np.isnan(min_age) or min_age < 18:
        category_string = category_string + 'P'

    if not((max_age <= 18) or (min_age > 65)):
        category_string = category_string + 'A'
    
    if np.isnan(max_age) or max_age > 65:
        category_string = category_string + 'S'

    return category_string

# Example usage
def main():
    sample_df = pd.read_excel("Silver Samples 4712.xlsx", "Age")
    sample_df['AgeCategory'] = sample_df.fillna("N/A").apply(lambda x: assign_age_group(x['Inclusion_agemin'], x['Inclusion_agemax']), axis=1)
    print(sample_df.head())

if __name__ == "__main__":
    main()