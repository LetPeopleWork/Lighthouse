import type React from "react";
import { useState } from "react";

/**
 * A tooltip that opens on hover and on any focus, not only a keyboard one: an element reached by a click
 * shows the words hovering over it already did, and a keyboard reader is never left guessing whether it
 * will. The target is put in the tab order, since a tooltip shown on focus is no use on an element a
 * keyboard cannot stop on. Escape closes an open tooltip without letting the dialog around it close too.
 */
export const useFocusTooltip = () => {
	const [isOpen, setIsOpen] = useState(false);

	const open = () => setIsOpen(true);
	const close = () => setIsOpen(false);

	// Stopping the key also keeps it from MUI's own listener, so the tooltip is closed here by hand.
	const closeOnEscape = (event: React.KeyboardEvent) => {
		if (event.key === "Escape" && isOpen) {
			event.stopPropagation();
			close();
		}
	};

	return {
		tooltip: { open: isOpen, onOpen: open, onClose: close },
		target: { tabIndex: 0, onFocus: open, onKeyDown: closeOnEscape },
	};
};
