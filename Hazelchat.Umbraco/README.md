# Hazelchat for Umbraco

Adds your [Hazelchat](https://hazelchat.co/?utm_source=umbraco&utm_medium=package&utm_campaign=readme) AI chat agent to every page Umbraco renders, from one setting. The agent answers visitors from the pages you approved, shows its sources and passes on their enquiries.

- Umbraco 13 to 18, on every template and every site in the installation. No template edits, no `Program.cs` changes.
- The code can differ per environment: leave it empty on staging and set it on live.
- Needs a Hazelchat account. The Free plan covers one website and stays free.

## Install

1. In Hazelchat, add your website and copy its installation code ([create a free account](https://app.hazelchat.co/signup?utm_source=umbraco&utm_medium=package&utm_campaign=readme-signup)). The install step shows the two lines below with your key filled in.
2. Add the package:

   ```bash
   dotnet add package Hazelchat.Umbraco
   ```

3. Add the code to `appsettings.json` (or set the environment variable `Hazelchat__InstallationCode`):

   ```json
   "Hazelchat": {
     "InstallationCode": "<your 64-character key>"
   }
   ```

4. Build and deploy as usual. Back in Hazelchat, click **Publish and switch on**. Hazelchat finds the chat on your live page and verifies the website.

**Settings → Health Check → Hazelchat** in the back office shows whether the code is set and valid.

## What it does

- Every page Umbraco renders from its content gets one tag just before `</body>`:
  `<script src="https://app.hazelchat.co/widget.js" id="hazelchat-umbraco" defer data-agent="<key>"></script>`
- `InstallationCode` may be the pasted code or just its 64-character key. Anything else, such as an API key, prints nothing and writes one warning to Umbraco's log at startup. The script address is fixed in the code.
- `RequireConsent` (`false`) adds `data-require-consent="true"`: the chat starts only once your consent manager allows it (Cookiebot, CookieTractor, Usercentrics and others). See [Visitor consent](https://hazelchat.co/integrations/umbraco/#consent).
- Only `GET` responses with status 200, `text/html` in UTF-8 and no compression, for pages Umbraco routed to published content, are changed. The back office, preview, APIs, media and other files, other controllers' pages and pages without `</body>` stay byte-for-byte as they are. Pages over 4 MB are sent on unchanged.
- A page that already has the tag, or where the installation code was pasted into a template by hand, gets no second one.
- Response compression belongs in front of Umbraco (`app.UseResponseCompression()` before `app.UseUmbraco()`, the usual place) or in your web server. If something compresses pages after Umbraco, the chat is not added.

## Data and third parties

- Visitors' browsers load the chat from `app.hazelchat.co` (Hazelchat, Germany). Hazelchat processes the questions they type and, if they leave an enquiry, their name, email address and message ([privacy notice](https://hazelchat.co/privacy/), [terms](https://hazelchat.co/terms/)).
- The chat sets no cookies. It keeps a reference to the conversation in session storage until the tab is closed (details on the [cookie banner page](https://hazelchat.co/integrations/cookie-banners/)).
- The package itself makes no requests from your site, adds no database tables, routes or back-office screens, and stores nothing.

## Remove

Remove the setting, or the package (`dotnet remove package Hazelchat.Umbraco`), and deploy. Your Hazelchat account and plan are not affected.

## Deutsch

Hazelchat fügt Ihren KI-Chat-Agenten in jede Seite ein, die Umbraco aus seinen Inhalten rendert (Umbraco 13 bis 18, ohne Template-Änderungen). Fügen Sie das Paket mit `dotnet add package Hazelchat.Umbraco` hinzu, tragen Sie den Installationscode aus Hazelchat in `appsettings.json` unter `Hazelchat:InstallationCode` ein und deployen Sie wie gewohnt. Die Browser Ihrer Besucher laden den Chat von `app.hazelchat.co`; deren Fragen und Anfragedaten verarbeitet Hazelchat. Sie brauchen ein Hazelchat-Konto; der Free-Tarif gilt für eine Website und bleibt kostenlos.

## Licence

MIT. Umbraco is a trademark of Umbraco A/S; Hazelchat is not affiliated with or endorsed by Umbraco.
