import os
import pandas as pd
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor


def UpdateDatabase():
    csv_file = Get_CSV_File()
    if csv_file is None:
        print("There should be exactly one CSV file in the input directory.  Please fix this and try again.")
        return
    
    columns = ['Artist Name', 'Artist Major', 'Project Title', 'Project Date', 'Project Genre', 'Project Description', 'Project Link']
    dataframe = pd.read_csv(csv_file, usecols=columns)
    
    BATCH_SIZE = 16
    for start in range(0, len(dataframe), BATCH_SIZE):
        print(f"Processing rows {start} to {min(start + BATCH_SIZE, len(dataframe))}...")
        batch = dataframe.iloc[start:start + BATCH_SIZE]
        
        for _, row in batch.iterrows():
            # TODO: Task 444
            # Get filepaths from project link scraper.GetFilepathsFromProjectLink(row["Project Link"]) (threaded)

            # TODO: Task 445
            # Insert all info into the database properly
            print(row)

def Get_CSV_File():
    INPUT_PATH = Path(os.getenv('INPUT_PATH'))
    csv_files = list(INPUT_PATH.glob("*.csv"))
    if (len(csv_files) != 1):
        return None
    return csv_files[0]
