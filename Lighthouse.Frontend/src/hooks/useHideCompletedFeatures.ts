import type React from "react";
import { useEffect, useState } from "react";

interface UseHideCompletedFeaturesResult {
	hideCompleted: boolean;
	handleToggleChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
}

export const useHideCompletedFeatures = (
	storageKey: string,
): UseHideCompletedFeaturesResult => {
	const [hideCompleted, setHideCompleted] = useState<boolean>(true);

	useEffect(() => {
		const storedPreference = localStorage.getItem(storageKey);
		if (storedPreference === null) {
			localStorage.setItem(storageKey, "true");
		} else {
			setHideCompleted(storedPreference === "true");
		}
	}, [storageKey]);

	const handleToggleChange = (
		event: React.ChangeEvent<HTMLInputElement>,
	): void => {
		const newValue = event.target.checked;
		setHideCompleted(newValue);
		localStorage.setItem(storageKey, newValue.toString());
	};

	return { hideCompleted, handleToggleChange };
};
