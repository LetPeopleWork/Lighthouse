## Set a Connection's Defaults Once, Not for Every Team

Every Team on a Jira or Azure DevOps connection that reads its parent from the same link needs its own Parent Override Field. Forget one on a new Team, and its Work Items sit unparented with nothing pointing at the cause. A Portfolio repeats the same choices for its Dependency Field, Size Estimate Field and Feature Owner Field. Change the field on the connection, and every one of them has to be found and changed by hand.

Those settings now live on the connection, once. Set them under **Defaults for Teams and Portfolios** on the connection page, and every Team and Portfolio that uses that connection follows the default. A Team or Portfolio can still own its own choice, including a deliberate None, and it keeps that choice when the default changes.

- **Jira and Azure DevOps** offer the Parent Override Field and Estimation for Teams, and the Parent Override Field, Dependency Field, Size Estimate Field, Feature Owner Field and Estimation for Portfolios.
- **CSV** offers Estimation for Teams, and the Feature Owner Field and Estimation for Portfolios.

Changing a default that Teams or Portfolios follow asks first. It counts the followers and offers **Save and update** or **Save only**, so you choose whether they update now or at their next scheduled update. An Estimation change needs no update: the followers show it the next time their pages load.

![Connection defaults](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/settings/worktrackingsystems/connection-defaults.png)

Full detail: [Defaults for Teams and Portfolios](https://docs.lighthouse.letpeople.work/settings/worktrackingsystems.html#defaults-for-teams-and-portfolios).

## Bugfixes and Improvements

- **A Portfolio's dependencies can come from a Jira link type or an Azure DevOps relation type.** A Portfolio whose waits are recorded with a link type other than the default showed none in Features → Dependencies. Name that link type, or the end of the relation type on which the work waits, as the Dependency Field, and Lighthouse reads the waits from it. Full detail: [Jira link types](https://docs.lighthouse.letpeople.work/concepts/worktrackingsystems/jira.html#naming-a-different-link-type) and [Azure DevOps relation types](https://docs.lighthouse.letpeople.work/concepts/worktrackingsystems/azuredevops.html#naming-a-different-relation-type).
- **A setting that pointed at a deleted field is repaired on upgrade.** A Team or Portfolio whose Parent Override Field, Dependency Field, Size Estimate Field, Feature Owner Field or Estimation named an Additional Field the connection no longer has now follows the connection's default. No default is set on upgrade, so it reads the tracker's own value again, such as its own parent or dependency link. Each of them downloads everything once at its next scheduled update.
- **Back up before you upgrade if you might go back.** An owner that follows a connection default has its own value cleared. An earlier version does not know defaults, so it reads the tracker's own field for that owner, which is empty. Your data is not damaged, but the value the owner held before is not recovered by upgrading again.
- **Scripts and API clients: a save that sends no source keeps the owner's choice.** Sending `null` for a Team's or Portfolio's setting no longer clears its own value. To change the value, send the source you read with it, for example `"parentOverrideSource": "Own"`. A script that saves back a value it read before the connection's default changed, without the source, makes that Team or Portfolio own the old value, so always send the source along.

## Contributions ❤️

Special thanks to everyone who contributed feedback for this release:
- [Steve Pereira](https://www.linkedin.com/in/devopsto/), who reported the connection defaults
