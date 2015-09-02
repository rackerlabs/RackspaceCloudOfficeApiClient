## Rackspace Cloud Office API Client

A C# API client library for [Rackspace Cloud
Office](http://www.rackspace.com/cloud-office).  See [the API
documentation](http://api-wiki.apps.rackspace.com/api-wiki/index.php/Main_Page)
for full details on the calls you can make.

Features:

- General purpose client for sending `GET`, `POST`, `PUT` and `DELETE` requests
- Handles creating the `X-Api-Signature` token for you
- Automatically throttles requests to a max of 30 per second
- Thread-safe, so you can call a single instance from as many threads as you want
- Accepts body data as .NET objects and encodes them for you
- Can deserialize responses to either a `dynamic` object or to a caller-supplied type
- [Simple, single-file implementation](Rackspace.CloudOffice/ApiClient.cs)

### Getting Started

#### Pre-requisites

- .NET 4.5 (Necessary for async/await)

#### Installation

From the NuGet Package Manager Console:

    Install-Package Rackspace.CloudOffice

#### API Keys

In order to make any API calls, you will need API keys.  If you need to
generate new ones, log in to the Cloud Office Control Panel, then go to the
[API keys page](https://cp.rackspace.com/MyAccount/Administrators/ApiKeys).

![API Keys screenshot](https://i.imgur.com/IigeLm2.png)

*Screenshot of the API keys page*

For convenience, __you can save your API keys to a config file__ so that you
don't have to pass them to the constructor every time.  The easiest way to do
this is to:

1. Download the [Invoke-RsCloudOffice.ps1
   script](https://github.com/rackerlabs/Invoke-RsCloudOfficeRequest)
1. Run the following command:

        .\Invoke-RsCloudOfficeRequest.ps1 -SaveConfig -UserKey pugSoulpxYmQDQiY6f1j -SecretKey bI4+E0cV93qigYKuC+sRAJkqyMlc6CThXr9CDXjc

(Replace the example keys with your actual keys)

When you are finished interacting with the API, you may optionally delete the
config file at `%LOCALAPPDATA\RsCloudOfficeApi.config` so that your keys aren't
left on the computer.

### ApiClient Reference

__Class Name:__ ApiClient

__Namespace:__ Rackspace.CloudOffice

#### Constructors

Name                        | Description
----------------------------|------------
`ApiClient(String, String)` | Initialize client with the specified *UserKey* and *SecretKey*
`ApiClient([String])`       | Initialize client using keys from the specified *ConfigFilePath* (defaults to `%LOCALAPPDATA%\RsCloudOfficeApi.config`)

#### Methods

Name                             | Description
---------------------------------|------------
`Get(String)`                    | Perform a `GET` API request to the specified *Path*
`GetAll(String, String)`         | Perform as many `GET`s as necessary to the paged listing at *Path* to unpage the named property *PagedProperty*
`Post(String, Object, [String])` | Perform a `POST` API request to the specified *Path*, sending *Data* encoded in *ContentType* format
`Put(String, Object, [String])`  | Perorm a `PUT` API request to the specified *Path*, sending *Data* encoded in *ContentType* format
`Delete(String)`                 | Perform a `DELETE` API request to the specified *Path*
