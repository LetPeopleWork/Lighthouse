## Bugfixes and Improvements

- **Updates that change nothing no longer recalculate your forecasts.** Every few minutes a background update re-ran the forecast of every Portfolio it touched, even when nothing had changed since the last one. With many Portfolios and hundreds of Features that meant minutes of simulation per update, and Lighthouse felt slow while it ran. Now, when nothing a Portfolio's forecast reads has changed, Lighthouse confirms the existing forecast in seconds instead of recalculating it: the dates stay, and the forecast's "last updated" time moves to that update. Anything you start yourself still recalculates, and so does the first forecast after Lighthouse starts. Settings → System Info → Refresh History marks these runs "skipped (nothing changed)" and leaves them out of the duration figures, so those keep telling you how long a real forecast takes.

## Contributions ❤️

Special thanks to everyone who contributed feedback for this release:
- [Steve Pereira](https://www.linkedin.com/in/devopsto/), whose logs showed every quiet update recalculating forecasts for minutes
