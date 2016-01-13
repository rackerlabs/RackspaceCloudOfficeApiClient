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

### Example Usage

All the examples assume you have created an instance of `ApiClient` as `client`.  For example:

```csharp
var client = new ApiClient();
```

#### Admins

__Note:__ In all the example URLs, replace `jane.doe` with the name of the
admin you want to act on.


##### List All Admins

```csharp
var admins = await client.GetAll<AdminListItem>("/v2/admins", "admins");
```

Where the `AdminListItem` type needs to look something like:

```csharp
public class AdminListItem
{
    public string AdminId { get; set; }
    public string Email { get; set; }
    public bool Enabled { get; set; }
    public bool Locked { get; set; }
    public string Type { get; set; }
}
```

##### Add An Admin

```csharp
var newAdmin = new {
    type = "super",                         // [required] "super", "standard", or "limited"
    password = "Password!1",                // [required]
    firstName = "Jane",                     // [required]
    lastName = "Doe",                       // [required]
    email = "jane.doe@example.com",         // [required]
    securityQuestion = "what is delicious", // [required]
    securityAnswer = "candy",               // [required]
    passwordExpiration = 90,                // in days
    allowSimultaneousLogins = true,
    restrictedIps = "192.0.2.2",
    enabled = true,
    locked = false,
};
await client.Post("/v2/admins/jane.doe", newAdmin);
```

##### Get Details On A Specific Admin

```csharp
var admin = await client.Get<Admin>("/v2/admins/jane.doe");
```

Where the `Admin` type needs to look something like:

```csharp
public class Admin
{
    public string AdminId { get; set; }
    public bool AllowSimultaneousLogin { get; set; }
    public string Email { get; set; }
    public bool Enabled { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool Locked { get; set; }
    public long PasswordExpiration { get; set; }
    public IList<string> RestrictedIps { get; set; }
    public string Type { get; set; }
}
```

##### Edit An Admin

```csharp
await client.Put("/v2/admins/jane.doe", new { firstName = "New Name" });
```

##### Delete An Admin

```csharp
await client.Delete("/v2/admins/jane.doe");
```

----

#### Domains

##### List All Domains

```csharp
var domains = await client.GetAll<DomainListItem>("/v2/domains", "domains");
```

where the `DomainListItem` type needs to look something like:

```csharp
public class DomainListItem
{
    public string AccountNumber { get; set; }
    public int ExchangeMaxNumMailboxes { get; set; }
    public long ExchangeUsedStorage { get; set; }
    public string Name { get; set; }
    public int RsEmailMaxNumberMailboxes { get; set; }
    public long RsEmailUsedStorage { get; set; }
    public ServiceTypes ServiceType { get; set; }

    public enum ServiceTypes
    {
        None,
        Both,
        Exchange,
        RsEmail,
    }
}
```

----

#### Exchange Mailboxes

__Note__: In all the example URLs, replace `example.com` with your domain and
replace `jane.doe` with the name of the mailbox to act on.

##### List All Mailboxes

```csharp
var mailboxes = await client.GetAll<MailboxListItem>("/v2/domains/example.com/ex/mailboxes", "mailboxes");
```

Where the `MailboxListItem` type needs to look something like:

```csharp
public class MailboxListItem
{
    public string DisplayName { get; set; }
    public string Name { get; set; }
}
```

##### Add A Mailbox

```csharp
var newMailbox = new {
    displayName = "Jane Doe",  // [required]
    password = "Password!1",   // [required]
    size = 10*1024,            // [required]
    isHidden = false,
    isPublicFolderAdmin = false,
    firstName = "Jane",
    lastName = "Doe",
    company = "ACME Widgets Inc.",
    department = "Sales",
    jobTitle = "",
    addressLine1 = "1234 Sycamore Ln",
    city = "Blacksburg",
    state = "Virginia",
    zip = "24060",
    country = "USA",
    businessNumber = "555-555-5555",
    pagerNumber = "555-555-5555",
    homeNumber = "555-555-5555",
    mobileNumber = "555-555-5555",
    faxNumber = "555-555-5555",
    notes = "",
    customID = "JDOE.1234",      // for your own use
    emailForwardingAddress = "", // empty = forwarding disabled
    visibleInRackspaceEmailCompanyDirectory = true,
    enabled = true,
};
await client.Post("/v2/domains/example.com/ex/mailboxes/jane.doe", newMailbox);
```

##### Get Details Of A Specific Mailbox

```csharp
var mailbox = await client.Get<Mailbox>("/v2/domains/example.com/ex/mailboxes/jane.doe", "mailboxes");
```

Where the `Mailbox` type needs to look something like:

```csharp
public class Mailbox
{
    public ContactInfoType ContactInfo { get; set; }
    public DateTime CreatedDate { get; set; }
    public long CurrentUsage { get; set; }
    public string DisplayName { get; set; }
    public IList<EmailAddress> EmailAddressList { get; set; }
    public string EmailForwardingAddress { get; set; }
    public bool Enabled { get; set; }
    public bool HasActiveSyncMobileService { get; set; }
    public bool HasBlackBerryMobileService { get; set; }
    public bool IsHidden { get; set; }
    public bool IsPublicFolderAdmin { get; set; }
    public DateTime? LastLogin { get; set; }
    public string Name { get; set; }
    public string SamAccountName { get; set; }
    public long Size { get; set; }
    public bool VisibleInRackspaceEmailCompanyDirectory { get; set; }

    public class EmailAddress
    {
        public string Address { get; set; }
        public string ReplyTo { get; set; }
    }

    public class ContactInfoType
    {
        public string AddressLine1 { get; set; }
        public string BusinessNumber { get; set; }
        public string City { get; set; }
        public string Company { get; set; }
        public string Country { get; set; }
        public string CustomId { get; set; }
        public string Department { get; set; }
        public string FaxNumber { get; set; }
        public string FirstName { get; set; }
        public string HomeNumber { get; set; }
        public string JobTitle { get; set; }
        public string LastName { get; set; }
        public string MobileNumber { get; set; }
        public string Notes { get; set; }
        public string PagerNumber { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
    }
}
```

##### Edit A Mailbox

```csharp
var mailboxEdits = new {
    firstName = 'New Name',
};
await client.Put("/v2/domains/example.com/ex/mailboxes/jane.doe", mailboxEdits);
```

##### Delete A Mailbox

```csharp
await client.Delete("/v2/domains/example.com/ex/mailboxes/jane.doe");
```

### See Also

- [Invoke-RsCloudOfficeRequest](https://github.com/rackerlabs/Invoke-RsCloudOfficeRequest) — a PowerShell client for the Cloud Office REST API for scripting and interactive usage
