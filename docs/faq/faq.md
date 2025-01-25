---
title: Frequently Asked Questions
layout: home
nav_order: 90
---

This section covers frequently asked questions and tries to answer them as good as possible. It's something we try to extend continuously to cover questions from the users.  

If you can't find answers to your questions neither in the documentation nor on this page, please check the [Contributions page](../contributions/contributions.html) to see how you can contribute by reaching out to us so we can improve Lighthouse!

<details markdown="block">
  <summary>
    How Do I Create Queries?
  </summary>
  {: .text-delta }
We use built-in functionality for our supported languages. If you struggle with creating queries, please check the respective documentation. We provided some examples for [Jira](../concepts/jira.html) and [Azure DevOps](../concepts/azuredevops.html). Apart from that, please rely on the official documentation.

For Jira, you may start here: [Use advanced search with Jira Query Language (JQL)](https://support.atlassian.com/jira-service-management-cloud/docs/use-advanced-search-with-jira-query-language-jql/)  

For Azure DevOps, check out the [Work Item Query Language (WIQL) syntax reference](https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops&viewFallbackFrom=vsts) and the [Wiql Editor Extension](https://marketplace.visualstudio.com/items?itemName=ms-devlabs.wiql-editor).

If you need support, consider joining [our Slack Community](https://join.slack.com/t/let-people-work/shared_invite/zt-2y0zfim85-qhbgt8N0yw90G1P~JWXvlg) where other users may be able to help you.
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
