# Acceptance brief: story\-6504\-childless\-feature\-size

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
With default sizing switched off, a Feature with no child Work Items is observed as size 0, with no invented count and no remaining work assigned to any Team; with default sizing on, behaviour is unchanged from today.
```

#### E2

##### Promises

P2

```text
With default sizing on and Default Feature Size 4, a Feature without child Work Items shows 4 remaining Work Items split across its Teams, with the existing default-size warning.
```

#### E3

##### Promises

P3

```text
With default sizing off, a Feature without child Work Items counts as 0 remaining in its own forecast and in a containing Delivery forecast, with no special forecast wording or handling visible.
```

#### E4

##### Promises

P4

```text
With default sizing off and Default Feature Size 4, a Feature without child Work Items shows 0 remaining, no count such as 0 of 4 appears, and no Team carries remaining work for it.
```

#### E5

##### Promises

P5

```text
With default sizing off, a Feature without child Work Items shows the note No child Work Items yet where the default-size warning icon sits, both in the Feature list and on the Delivery timeline, and no default-Feature-size sentence appears.
```

#### E6

##### Promises

P6

```text
With default sizing off, a Feature in an override state such as In Progress shows its real child count of 1, not 4, and is not marked as using the default size.
```

#### E7

##### Promises

P7

```text
With default sizing off, Default Feature Size, the percentile option, the size-estimate field mapping and the override-states selection are greyed out and cannot be changed; switching sizing back on shows 7, the same mapping and the same states.
```

#### E8

##### Promises

P8

```text
Portfolios that existed before this change show default sizing switched on next to Default Feature Size, and a newly created Portfolio does the same, with Feature counts unchanged from today.
```

## Charter

None.

## Examination

None.
