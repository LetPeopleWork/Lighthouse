// __SCAFFOLD__ - DISTILL placeholder for Epic #5511 slice 02 (#5840).
//
// The header's activity icon and the popover it opens: what the instance is refreshing and what is
// waiting. DELIVER replaces this with the real component; DESIGN splits the popover out as
// TaskManagerPopover, which is a structure decision this scaffold deliberately does not make.
//
// It throws rather than rendering nothing, so a specification that reaches it fails loudly instead of
// quietly agreeing that there is nothing to show.

const TaskManagerIcon = (): never => {
	throw new Error("Not yet implemented - RED scaffold (Epic #5511 slice 02)");
};

export default TaskManagerIcon;
