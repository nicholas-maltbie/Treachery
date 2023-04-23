import csv
import os
import json
import uuid

from pathlib import Path
from datetime import datetime

seconds_per_month = 2628000

def main():
    file_path = os.path.realpath(__file__)
    file_folder = str(Path(file_path).parent.absolute())
    filename = file_folder + "\\results.csv"
    manual_list = file_folder + "\\manual-sub-list.csv"

    # initializing the titles and rows list
    fields = []
    
    subscribers = dict()
    userId_by_name = dict()

    # reading csv file
    with open(filename, 'r', encoding="utf-8") as csvfile:
        # creating a csv reader object
        csvreader = csv.reader(csvfile)
        
        # extracting field names through first row
        fields = next(csvreader)
    
        now = datetime.utcnow()

        # extracting each data row one by one
        for row in csvreader:
            # get the user Id
            entryType = row[0]
            userId = row[1]
            userName = row[2]
            timeInteract = row[3]

            userId_by_name[userName] = userId

            try:
                dt_obj = datetime.strptime(timeInteract, r"%Y-%m-%dT%H:%M:%S.%fZ")
            except:
                dt_obj = datetime.strptime(timeInteract, r"%Y-%m-%dT%H:%M:%SZ")
            delta = now - dt_obj
            month_sub = round(delta.total_seconds() / seconds_per_month, 5)

            if userId not in subscribers:
                subscribers[userId] = {"name": userName, "months": month_sub, "strength": 1}
            else:
                sub = subscribers[userId]
                if month_sub > sub["months"]:
                    sub["months"] = month_sub
                sub["strength"] += 1

    # reading csv file
    with open(manual_list, 'r', encoding="utf-8") as csvfile:
        # creating a csv reader object
        csvreader = csv.reader(csvfile)
        
        # extracting field names through first row
        fields = next(csvreader)
    
        now = datetime.utcnow()

        # extracting each data row one by one
        for row in csvreader:
            # get the user Id
            entryType = row[0]
            userId = str(uuid.uuid4())
            userName = row[2]
            timeInteract = row[3]

            if userName in userId_by_name:
                userId = userId_by_name[userName]

            dt_obj = datetime.strptime(timeInteract, r"%Y-%m-%dT%H:%M:%SZ")
            delta = now - dt_obj
            month_sub = round(delta.total_seconds() / seconds_per_month, 5)

            if userId not in subscribers:
                subscribers[userId] = {"name": userName, "months": month_sub, "strength": 1}
            else:
                sub = subscribers[userId]
                if month_sub > sub["months"]:
                    sub["months"] = month_sub
                sub["strength"] += 1

    sub_data = [subscribers[userId] for userId in subscribers]
    with open(file_folder + '\\subscribers.json', 'w') as f:
        json.dump({"subs": sub_data}, f)

if __name__ == "__main__":
    main()
