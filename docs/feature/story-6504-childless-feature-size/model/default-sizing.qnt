// Sizing rule of one Feature as designed for Portfolio.SizingFor (designed, not built).
module defaultsizing {
  type Out = { size: int, assigned: bool, usingDefault: bool }

  pure val DEFAULT = 4

  // Infer off: real children or 0. No default, no estimate, no override rule, no Team assignment.
  pure def sizing(infer: bool, done: bool, children: int, ovr: bool, est: int): Out = {
    val cleared = infer and ovr and children < DEFAULT
    val effective = if (cleared) 0 else children
    val extrapolate = infer and not(done) and effective == 0
    if (extrapolate) { size: if (est > 0) est else DEFAULT, assigned: true, usingDefault: true }
    else { size: effective, assigned: effective > 0, usingDefault: false }
  }

  var infer: bool
  var done: bool
  var children: int
  var ovr: bool
  var est: int

  action init = all {
    infer' = true, done' = false, children' = 0, ovr' = false, est' = 0,
  }

  action step = {
    nondet i = oneOf(Set(true, false))
    nondet d = oneOf(Set(true, false))
    nondet c = oneOf(0.to(6))
    nondet v = oneOf(Set(true, false))
    nondet e = oneOf(0.to(9))
    all { infer' = i, done' = d, children' = c, ovr' = v, est' = e }
  }

  val o = sizing(infer, done, children, ovr, est)

  val offInventsNothing = not(infer) implies not(o.usingDefault)
  val offSizeIsRealChildren = not(infer) implies (o.size == children)
  val offAssignsOnlyRealWork = not(infer) implies (o.assigned == (children > 0))
  val onChildlessIsInferred = (infer and not(done) and children == 0) implies
    (o.usingDefault and o.size == (if (est > 0) est else DEFAULT))
  val onWithRealChildrenUntouched = (infer and children >= DEFAULT) implies (o.size == children)
}
