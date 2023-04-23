# -*- coding: utf-8 -*-

# Sample Python code for youtube.subscriptions.list
# See instructions for running these code samples locally:
# https://developers.google.com/explorer-help/code-samples#python

import os

import google_auth_oauthlib.flow
import googleapiclient.discovery
import googleapiclient.errors

from pathlib import Path

scopes = ["https://www.googleapis.com/auth/youtube.readonly", "https://www.googleapis.com/auth/youtube.force-ssl"]

def main():
    file_path = os.path.realpath(__file__)
    file_folder = str(Path(file_path).parent.absolute())
    client_id_path = file_folder + "\\secrets\\client-id.json"

    result_file_path = file_folder + "\\results.csv"
    if not os.path.exists(result_file_path):
        with open(result_file_path, 'w') as f:
            f.write("Type,UserId,UserName,Time\n")

    result_file = open(result_file_path, "a", encoding='utf_8')

    # Disable OAuthlib's HTTPS verification when running locally.
    # *DO NOT* leave this option enabled in production.
    os.environ["OAUTHLIB_INSECURE_TRANSPORT"] = "1"

    api_service_name = "youtube"
    api_version = "v3"
    client_secrets_file = file_folder + "\\secrets\\client-id.json"
    client_key_file = file_folder + "\\secrets\\api-key.txt"

    with open(client_key_file, 'r') as file:
        api_key = file.read().replace('\n', '')
    
    print("API_KEY: " + api_key)

    # Get credentials and create an API client
    flow = google_auth_oauthlib.flow.InstalledAppFlow.from_client_secrets_file(
        client_secrets_file, scopes)
    credentials = flow.run_console()
    youtube = googleapiclient.discovery.build(
        api_service_name, api_version, credentials=credentials)

    myChannelId = "UCoGYwhYV-ar1d7b3niqqDQw"

    subIds = dict()
    def check_user_subscribed(userId):
        if userId in subIds:
            return

        subIds[userId] = 1
        request = youtube.subscriptions().list(
            part="subscriberSnippet,snippet",
            channelId=userId,
            forChannelId=myChannelId
        )
        
        # check if the commenter is subscribed
        try:
            checkResponse = request.execute()
        except:
            # if their subscription list is private, this may throw an error
            return

        if int(checkResponse["pageInfo"]["totalResults"]) == 1:
            for sub in checkResponse["items"]:
                subname = sub["subscriberSnippet"]["title"]
                subid = sub["subscriberSnippet"]["channelId"]
                subdate = sub["snippet"]["publishedAt"]

                line = ("\"subscriber\",\"" + subid + "\",\"" + subname + "\",\"" + subdate + "\"\n").encode("utf-8").decode("utf-8")
                result_file.write(line)

    next_page_token = ''
    while True:
        request = youtube.subscriptions().list(
            part="snippet,subscriberSnippet",
            maxResults=50,
            mySubscribers=True,
            pageToken=next_page_token
        )
        response = request.execute()

        # get sub list
        for sub in response['items']:
            subname = sub["subscriberSnippet"]["title"]
            subid = sub["subscriberSnippet"]["channelId"]
            subdate = sub["snippet"]["publishedAt"]
            subIds[subid] = 1

            line = ("\"subscriber\",\"" + subid + "\",\"" + subname + "\",\"" + subdate + "\"\n").encode("utf-8").decode("utf-8")
            result_file.write(line)

        if 'nextPageToken' in response:
            next_page_token = response['nextPageToken']
        else:
            next_page_token = ''

        if not next_page_token:
            break

    # Get all the comments on the channel
    next_page_token = ''
    while True:
        request = youtube.commentThreads().list(
            part="id,replies,snippet",
            maxResults=100,
            moderationStatus="published",
            order="orderUnspecified",
            textFormat="plainText",
            allThreadsRelatedToChannelId=myChannelId,
            pageToken=next_page_token
        )
        response = request.execute()

        for commentThread in response["items"]:
            snippet = commentThread["snippet"]
            topLevelComment = snippet["topLevelComment"]
            authorId = topLevelComment["snippet"]["authorChannelId"]["value"]
            authorName = topLevelComment["snippet"]["authorDisplayName"]
            time = topLevelComment["snippet"]["publishedAt"]
            
            line = ("\"" + "comment" + "\",\"" + authorId + "\",\"" + authorName + "\",\"" + time + "\"\n").encode("utf-8").decode("utf-8")
            result_file.write(line)
            check_user_subscribed(authorId)

            # get all the replies to that top level comment as well.
            if "replies" in commentThread:
                for reply in commentThread["replies"]["comments"]:
                    authorId = reply["snippet"]["authorChannelId"]["value"]
                    authorName = reply["snippet"]["authorDisplayName"]
                    time = reply["snippet"]["publishedAt"]
                    line = ("\"" + "comment" + "\",\"" + authorId + "\",\"" + authorName + "\",\"" + time + "\"\n").encode("utf-8").decode("utf-8")
                    result_file.write(line)
                    check_user_subscribed(authorId)

        if 'nextPageToken' in response:
            next_page_token = response['nextPageToken']
        else:
            next_page_token = ''

        if not next_page_token:
            break

    result_file.close()

if __name__ == "__main__":
    main()
