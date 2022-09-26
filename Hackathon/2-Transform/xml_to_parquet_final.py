#!/usr/bin/env python
# coding: utf-8

# ## xml_to_parquet_final
# 
# 
# 

# In[ ]:


from pyspark.sql import SparkSession

SparkSession.builder.getOrCreate()


# In[24]:


xml_path = 'abfss://raw1xml@synstrg.dfs.core.windows.net/*.xml' #NCT05504733


# In[25]:


df = spark.read.format("com.databricks.spark.xml").option("rowTag","clinical_study").option("attributePrefix","__").load(xml_path)
df.count()
#display(df)


# In[26]:


from pyspark.sql.functions import col, explode


def type_cols(df_dtypes, filter_type):
    cols = []
    for col_name, col_type in df_dtypes:
        if col_type.startswith(filter_type):
            cols.append(col_name)
    return cols


# In[ ]:


struct_cols = type_cols(df.dtypes,"struct")

struct_cols


# In[ ]:


array_cols = type_cols(df.dtypes,"array")

array_cols


# In[27]:


from pyspark.sql.functions import col


def flatten_df1(nested_df):
    stack = [((), nested_df)]
    columns = []

    while len(stack) > 0:
        parents, df = stack.pop()

        flat_cols = [
            col(".".join(parents + (c[0],))).alias("_".join(parents + (c[0],)))
            for c in df.dtypes
            if c[1][:6] != "struct"
        ]

        nested_cols = [
            c[0]
            for c in df.dtypes
            if c[1][:6] == "struct"
        ]

        columns.extend(flat_cols)

        for nested_col in nested_cols:
            if nested_col != 'clinical_results':
                projected_df = df.select(nested_col + ".*")
                stack.append((parents + (nested_col,), projected_df))

    return nested_df.select(columns)


# In[ ]:


df_flat =flatten_df1(df)

display(df_flat.limit(10))


# In[ ]:


spark.conf.set('spark.sql.execution.arrow.pyspark.enabled', 'False')

pd_df = df_flat.toPandas()

print(pd_df)


# In[30]:


pd_df.to_csv('abfss://syncstrg@synstrg.dfs.core.windows.net/output/clinical_trial.csv', header= True, sep=',')


# In[31]:


pd_df.to_excel('abfss://syncstrg@synstrg.dfs.core.windows.net/output/clinical_trial.xlsx', header= True)


# In[32]:


path = "/output/flattenoutput"
df_flat.write.format("parquet").mode("overwrite").save(path)


# In[33]:


df_sing = df_flat.coalesce(1)

path_sing = "/output/singleparquet"

df_sing.write.format("parquet").mode("overwrite").save(path_sing)

