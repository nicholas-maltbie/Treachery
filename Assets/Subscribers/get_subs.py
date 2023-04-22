# Import the necessary libraries
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError
from oauth2client.tools import argparser

# Define the API endpoint and API version
api_service_name = "youtube"
api_version = "v3"

# Set API key
api_key = input("Your API KEY")

# Create the YouTube API client
youtube = build(api_service_name, api_version, developerKey=api_key)

# Define the channel ID for the channel you want to get subscribers for
channel_id = "UCoGYwhYV-ar1d7b3niqqDQw"

# Define the API request to get the subscriber list for the channel
subscriber_response = youtube.subscriptions().list(
    part="subscriberSnippet",
    channelId=channel_id,
    maxResults=50
).execute()

# Loop through each page of subscribers and add them to a list
subscribers = []
while subscriber_response:
    for subscriber in subscriber_response["items"]:
        subscribers.append(subscriber["subscriberSnippet"]["title"])
    if "nextPageToken" in subscriber_response:
        subscriber_response = youtube.subscriptions().list(
            part="subscriberSnippet",
            channelId=channel_id,
            maxResults=50,
            pageToken=subscriber_response["nextPageToken"]
        ).execute()
    else:
        break

# Print the list of subscribers
print(subscribers)