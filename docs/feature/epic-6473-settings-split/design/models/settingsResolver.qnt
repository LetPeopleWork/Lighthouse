module settingsResolver {
  // Areas: 0 none requested (bare /settings), 1 organization, 2 access, 3 system, 4 unknown slug
  // Outcome kinds: 0 pending, 1 wait, 2 show, 3 redirect
  var loading: bool
  var admin: bool
  var req: int
  var kind: int
  var target: int

  pure def visible(adm: bool, a: int): bool =
    if (a == 1) adm else (a == 2 or a == 3)

  pure def firstVisible(adm: bool): int = if (adm) 1 else 2

  action init = all {
    loading' = true, admin' = true, req' = 0, kind' = 0, target' = 0,
  }

  action accessLoaded(adm: bool): bool = all {
    loading, loading' = false, admin' = adm, req' = req, kind' = kind, target' = target,
  }

  action ask(r: int): bool = all {
    req' = r, loading' = loading, admin' = admin, kind' = kind, target' = target,
  }

  action resolve = all {
    if (loading) all { kind' = 1, target' = 0 }
    else if (req >= 1 and req <= 3 and visible(admin, req)) all { kind' = 2, target' = req }
    else all { kind' = 3, target' = firstVisible(admin) },
    loading' = loading, admin' = admin, req' = req,
  }

  action step = any {
    accessLoaded(true), accessLoaded(false),
    ask(0), ask(1), ask(2), ask(3), ask(4),
    resolve,
  }

  // never redirect or show before access has loaded
  val noDecisionWhileLoading = loading implies (kind == 0 or kind == 1)
  // a shown or redirected area is always one the person may see
  val targetVisible = (kind == 2 or kind == 3) implies visible(admin, target)
  // an admin on a bare address lands on organization, never on a later area
  val adminBareLandsOnOrganization = (kind == 3 and admin and req == 0) implies target == 1
}
