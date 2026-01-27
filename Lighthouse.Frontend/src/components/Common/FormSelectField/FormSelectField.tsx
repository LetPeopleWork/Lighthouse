import type { SelectChangeEvent } from "@mui/material";
import { FormControl, InputLabel, MenuItem, Select } from "@mui/material";
import type React from "react";

interface FormSelectFieldProps<T extends string | number> {
	label: string;
	value: T | "";
	onChange: (value: T | null) => void;
	options: Array<{ id: T; label: string }>;
	allowNone?: boolean;
	noneLabel?: string;
	fullWidth?: boolean;
	margin?: "none" | "dense" | "normal";
}

function FormSelectField<T extends string | number>({
	label,
	value,
	onChange,
	options,
	allowNone = true,
	noneLabel = "None",
	fullWidth = true,
	margin = "normal",
}: Readonly<FormSelectFieldProps<T>>): React.ReactElement {
	const handleChange = (e: SelectChangeEvent<T | "">) => {
		const selectedValue = e.target.value;
		onChange(selectedValue === "" ? null : (selectedValue as T));
	};

	return (
		<FormControl fullWidth={fullWidth} margin={margin}>
			<InputLabel>{label}</InputLabel>
			<Select<T | ""> value={value} label={label} onChange={handleChange}>
				{allowNone && (
					<MenuItem value="">
						<em>{noneLabel}</em>
					</MenuItem>
				)}
				{options.map((option) => (
					<MenuItem key={option.id} value={option.id}>
						{option.label}
					</MenuItem>
				))}
			</Select>
		</FormControl>
	);
}

export default FormSelectField;
