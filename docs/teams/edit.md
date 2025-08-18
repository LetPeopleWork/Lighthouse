---
title: Create/Edit Teams
layout: home
parent: Teams
nav_order: 2
---

Whether you want to create a new team or edit an existing one, you can use this page to specify all the details that make up your team.

- TOC
{:toc}

# Validation and Save
Before you can save a new or modified team, you'll have to *Validate* the changes. Lighthouse will run a query against your specified work tracking system and make sure that the query is successfully executing. Only after this you will be able to save.

The validation checks the following things:
- The connection to the work tracking system is valid (the connection settings are ok)
- The query can be executed (the query is having a correct syntax)
- The query returns at least one closed item (we must have a *Throughput* in order to forecast, so we need at least one closed item)

If the validation is ok, you are good to save the changes.

# General Configuration
The general information contains the name of your team. This can be anything that helps you identify it.

{: .recommendation}
We suggest to use the same name as you use in your work tracking system to identify your team.

## Throughput
