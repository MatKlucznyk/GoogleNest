# GoogleNest
Crestron S# library for controlling google nest devices

Current code supports only thermostats for 3-series and 4-series. I will be creating a branch for 4-series only that will support more events from thermostats and other devices.

To get started you will need to enable the Smart Device Managment API for the Device Access Sandbox through this link: https://developers.google.com/nest/device-access
There is a one time fee of $5 for 25 user accounts across 5 structures.

Next you'll need to create an Oauth2 client ID and Client Secret at this link: https://console.developers.google.com/apis/credentials

Copy the client ID and Client Secret and head back to the Device Access Sandbox at this link: https://console.nest.google.com/device-access/project-list

Create a project and copy the Oauth2 client ID into Client ID field. Copy the Client ID, Client Secret and Project ID and enter them into the Google Nest System Processor module.

To get an auth code, change the project-id field and the oauth2-client-id parameter you've copied down and follow the link;  https://nestservices.google.com/partnerconnections/project-id/auth?redirect_uri=https://www.google.com&access_type=offline&prompt=consent&client_id=oauth2-client-id&response_type=code&
scope=https://wwwgoogleapis.com/auth/sdm.service

Once you return to the google page, in the URI, you'll find the auth code. Copy it down and send the full code to the AuthCode serial signal. Next trigger the Initialize signal. Once the initial initialization is complete, a refresh token will take care of authorizing from here on out as long as the processor is online once every six months.
