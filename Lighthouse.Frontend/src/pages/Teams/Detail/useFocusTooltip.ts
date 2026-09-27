import { useEffect, useState } from "react";

/**
 * A tooltip that opens on hover and on any focus, not only a keyboard one: an element reached by a click
 * shows the words hovering over it already did, and a keyboard reader is never left guessing whether it
 * will. The target is put in the tab order, since a tooltip shown on focus is no use on an element a
 * keyboard cannot stop on. Escape closes an open tooltip without letting the dialog around it close too,
 * wherever the focus is, since a tooltip opened by hovering leaves the focus somewhere else.
 */
export const useFocusTooltip = () => {
	const [isOpen, setIsOpen] = useState(false);

	useEffect(() => {
		if (!isOpen) {
			return;
		}
		// Caught on the way down, before the dialog's own handler can see it. Stopping it there also keeps
		// it from MUI's tooltip listener, so the tooltip is closed here by hand.
		const closeOnEscape = (event: KeyboardEvent) => {
			if (event.key === "Escape") {
				event.stopPropagation();
				setIsOpen(false);
			}
		};
		document.addEventListener("keydown", closeOnEscape, true);
		return () => document.removeEventListener("keydown", closeOnEscape, true);
	}, [isOpen]);

	const open = () => setIsOpen(true);
	const close = () => setIsOpen(false);

	return {
		tooltip: { open: isOpen, onOpen: open, onClose: close },
		target: { tabIndex: 0, onFocus: open },
	};
};
