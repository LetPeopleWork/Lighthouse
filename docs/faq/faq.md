---
title: Frequently Asked Questions
layout: home
nav_order: 90
---

This section covers frequently asked questions and tries to answer them as good as possible. It's something we try to extend continuously to cover questions from the users.  

If you can't find answers to your questions neither in the documentation nor on this page, please check the [Contributions page](../contributions/contributions.html) to see how you can contribute by reaching out to us so we can improve Lighthouse! If you need support, also consider joining [our Slack Community](https://join.slack.com/t/let-people-work/shared_invite/zt-2y0zfim85-qhbgt8N0yw90G1P~JWXvlg) where other users may be able to help you.

## FAQ

<details markdown="block">
  <summary>
    How Do I Create Queries?
  </summary>
  {: .text-delta }
We use built-in functionality for our supported languages. If you struggle with creating queries, please check the respective documentation. We provided some examples for [Jira](../concepts/jira.html) and [Azure DevOps](../concepts/azuredevops.html). Apart from that, please rely on the official documentation.

For Jira, you may start here: [Use advanced search with Jira Query Language (JQL)](https://support.atlassian.com/jira-service-management-cloud/docs/use-advanced-search-with-jira-query-language-jql/)  

For Azure DevOps, check out the [Work Item Query Language (WIQL) syntax reference](https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops&viewFallbackFrom=vsts) and the [Wiql Editor Extension](https://marketplace.visualstudio.com/items?itemName=ms-devlabs.wiql-editor).
</details>

<details markdown="block">
  <summary>
    Do you support other Systems than Azure DevOps and Jira?
  </summary>
  {: .text-delta }
Lighthouse is built in so it can be easily extended with new Work Tracking System. Right now it supports Jira and Azure DevOps, as they are used the most and we can test against those systems.

If you use a different system, please reach out and we can see if it can be onboarded. While we can't make any promises, we are in general open to the idea!
</details>

<details markdown="block">
  <summary>
    How can I use a custom field for the Ordering?
  </summary>
  {: .text-delta }
Right now there is no way to use another field for ordering. By the way [Lighthouse forecasts](../concepts/howlighthouseforecasts.html), there has to be a **unique** order of Features **across the whole work tracking system**. Custom fields tend to not support this, and we would end up with ambigious orders (if two features have an *OrderIndex* of 12, which one would you expect to be forecasted earlier...).  
Thus we rely on the built-in mechanisms, which guarantee a unique order. While this may not be what you want to hear, you could use this as an opportunity to streamline your backlog and create transparency about the fact that the order does not seem to be taken care of.
</details>

<details markdown="block">
  <summary>
    Can we use other percentiles than 50/70/85/95%?
  </summary>
  {: .text-delta }
No, right now the percentiles are fix. In future this may become configurable, please let us know if this is something you'd need.
</details>

<details markdown="block">
  <summary>
    I just want to know how long a project takes if we were to start today, excluding other projects. How do I do that with Lighthouse?
  </summary>
  {: .text-delta }
Lighthouse will always take **the full Feature Backlog** into account (based on all defined projects). So you can't just *ignore* this. If you really want to do that, just create a single project. Please check [How Lighthouse Forecasts](../concepts/howlighthouseforecasts.html) for details on why we always use the full backlog.
</details>

<details markdown="block">
  <summary>
    Can we use Story Points instead of Throughput?
  </summary>
  {: .text-delta }
No.
</details>

<details markdown="block">
  <summary>
    How much data do I need to get started?
  </summary>
  {: .text-delta }
In our experience, you get decent results with as little as two weeks of data. It may not be perfect, but it's better than nothing.
</details>

<details markdown="block">
  <summary>
    How much 'History' should I use for my forecasts?
  </summary>
  {: .text-delta }
This of course depends on your context. We do recommend values between 30 and 90 days, as during this time, you'll most likely have a good and stable sample size, while also still being fairly recent. You could go back 6 months or 2 years, but is your team operating the same way as you did 2 years ago (and if the answer is yes, you may want to invest in continuous improvement).

{: .note}
In case of 'special events' (like the Christmas period in Western Europe), where the full team is off for a prolonged time, we propose to extend the regular period (so instead of 30 days may go up to 60 or 90) to "soften" to the impact of these 'no throughput' days.
</details>

<details markdown="block">
  <summary>
    Can we not simply forecast Feature Completion instead of relying on the child items?
  </summary>
  {: .text-delta }
In theory you can do that. However, there is a catch. Right now, Lighthouse only supports *Days* as the unit of time. Most likely you will not manage to close items on *Feature Level* on most days, leading to many '0 Throughput Days', making the accuracy of the forecasts very bad.  
</details>

<!-- FAQ Template

<details markdown="block">
  <summary>
    Question?
  </summary>
  {: .text-delta }
Content
</details>

-->