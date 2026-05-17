import {
	FormControl,
	InputLabel,
	MenuItem,
	Select,
	type SelectChangeEvent,
} from "@mui/material";
import { type FC, useId } from "react";
import type { IAuthenticationMethod } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

interface AuthMethodDropdownProps {
	methods: IAuthenticationMethod[];
	selectedKey: string;
	canUsePremiumFeatures: boolean;
	onChange: (key: string) => void;
	label?: string;
}

const isLocked = (
	method: IAuthenticationMethod,
	canUsePremiumFeatures: boolean,
): boolean => Boolean(method.isPremium) && !canUsePremiumFeatures;

const renderLabel = (
	method: IAuthenticationMethod,
	canUsePremiumFeatures: boolean,
): string =>
	isLocked(method, canUsePremiumFeatures)
		? `${method.displayName} (Premium)`
		: method.displayName;

const AuthMethodDropdown: FC<AuthMethodDropdownProps> = ({
	methods,
	selectedKey,
	canUsePremiumFeatures,
	onChange,
	label = "Authentication Method",
}) => {
	const labelId = useId();

	const handleChange = (event: SelectChangeEvent<string>) => {
		onChange(event.target.value);
	};

	return (
		<FormControl fullWidth>
			<InputLabel id={labelId}>{label}</InputLabel>
			<Select
				labelId={labelId}
				value={selectedKey}
				onChange={handleChange}
				label={label}
			>
				{methods.map((method) => {
					const locked = isLocked(method, canUsePremiumFeatures);
					return (
						<MenuItem
							key={method.key}
							value={method.key}
							sx={
								locked
									? { color: "text.disabled", fontStyle: "italic" }
									: undefined
							}
							aria-disabled={locked || undefined}
						>
							{renderLabel(method, canUsePremiumFeatures)}
						</MenuItem>
					);
				})}
			</Select>
		</FormControl>
	);
};

export default AuthMethodDropdown;
