---
title: Work Tracking Systems
layout: home
parent: Settings
nav_order: 1
---

# Work Tracking Systems
While you can add new Work Tracking Systems via the [Teams](../teams/edit.html#work-tracking-system) and [Project Creation Pages](../projects/edit.html#work-tracking-system), you can manage all Work Tracking Systems via the settings.

![Work Tracking Systems](../../assets/settings/worktrackingsystems.png)

## Adding New Work Tracking Systems
You can set up new Connections via the *Add Connection* button, and have to provide the details according to the selected Work Tracking System Type.
See [the concepts](../../concepts/concepts.html#work-tracking-system) for more details and how to specify it for your specific system.

## Modifying Existing Systems
It may happen that you want to adjust your existing connections. For example if the URL changes, you want to adjust your connection details (due to an updated token), or simply want to rename it.
You can do so by clicking on the 🖊️ icon on the right side of the work tracking system, and then adjust your settings as needed.

{: .important}
As the secret information (like API Tokens) are not available to the end user, you will **always** have to provide this information again on any change.

## Deleting Systems
You can also delete Work Tracking Systems if they are not needed anymore. To do so, you can click on the 🗑️ icon on the right side of the work tracking system. This will permantenly delete this work tracking system.

{: .note}
You can only delete a Work Tracking System if no team and project is using this. Either remove those teams and projects, or change them to use a different work tracking system.

## Work Tracking System Settings
There are settings that apply to any configured Work Tracking System setting. These you can find below the available and configured systems. Those settings are *advanced* settings, and in general, you ideally never need to adjust them. However, there are situations where this may be coming in handy.

### Request Timeout
You can override the default timeout for the requests that are made to your Work Tracking System. If you do so, it means that it Lighthouse will wait potentially longer for an answer from your system.

This can be useful if you're using a query that returns many items, and your system is not very fast in responding. This is more likely to happen if you're using an internally hosted system (for example Jira Data Center) which may not run on the fastest hardware.

To override, simply toggle on the override button, and specify the desired timeout in seconds. The default timeout when no override is active is 100 seconds. So if you override, you most likely want to be above that.