---
title: Metrics
layout: home
has_children: true
nav_order: 30
---

Lighthouse collects metrics about your team, so you can inspect those numbers whenever needed and create experiment to improve your efficiency, effectiveness, and predictability.

# About the Metrics
In general the metrics collected are the same  (with some differences) for *Teams* and *Projects*. Lighthouse is using the respective settings of a team or project to get the information. It looks at all work items that fit your *Work Item Query*, takes into account the *Work Item Types*, and categorizes items based on the configured *States*.

{: .note}
Many metrics can only be collected on finished items. It is therefore important to make sure **not to exclude** done items in your query if you want to use the metrics.

## Flow Metrics
The foundation of the metrics are the measures of flow as defined in the [Kanban Guide](https://kanbanguides.org/english/#elementor-toc__heading-anchor-10):

{: .definition}
**Work in Progress (WIP)**: The number of work items started but not finished.  
**Throughput**: The number of work items finished per unit of time. Note the measurement of throughput is the exact count of work items.  
**Work Item Age**: The amount of elapsed time between when a work item started and the current time.  
**Cycle Time**: The amount of elapsed time between when a work item started and when a work item finished.  

Each [widget](./widgets.html) will focus on one or more of those Flow Metrics.