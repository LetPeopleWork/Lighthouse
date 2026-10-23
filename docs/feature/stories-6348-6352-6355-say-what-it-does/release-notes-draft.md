## Bugfixes and Improvements

- **Closed Items Cutoff says what it limits.** Until now you found out what the cutoff does when a report refused the window you asked for. The line under **Closed Items Cutoff (days)** on a Team's and a Portfolio's settings now says it right where you set it: finished Work Items (or Features, on a Portfolio) older than this many days are not kept, which limits metrics, forecasts and how far back Reports can reach. 0 keeps them all, and raising it brings older ones back with the next update.
- **Dependency Field and Parent Override Field say which way round they read.** Picking the wrong end of a link, such as *blocks* instead of *is blocked by*, matched nothing and quietly hid your tracker's own links. An info button beside both fields, on a Team's and a Portfolio's settings and on the connection's defaults, now says which end the field reads, with an example for Jira and Azure DevOps, what None does, and a **Learn More** link to the docs.
- **The Release picker says why a Release cannot be picked, and that you can search.** With hundreds of Releases, the greyed-out ones under "No Release Date Set" made the list look broken. A Release without a date now reads "No date in Jira: set one there to follow it", and the empty picker reads "Type to search Jira Releases", so you can find the one you mean by typing.

## Contributions ❤️

Special thanks to everyone who contributed feedback for this release:
- [Steve Pereira](https://www.linkedin.com/in/devopsto/), who asked for the Closed Items Cutoff, dependency direction and Release picker hints
