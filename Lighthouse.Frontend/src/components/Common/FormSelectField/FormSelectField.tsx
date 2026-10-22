import type { SelectChangeEvent } from "@mui/material";
import {
	FormControl,
	FormHelperText,
	InputLabel,
	MenuItem,
	Select,
} from "@mui/material";
import type React from "react";
import { useId } from "react";

interface SelectOption<T> {
	id: T;
	label: string;
}

/** What an info icon beside the field explains, and where the docs say more. */
export interface FieldInfo {
	text: string;
	learnMoreUrl?: string;
}

interface FormSelectFieldProps<T extends string | number> {
	label: string;
	value: T | "";
	onChange: (value: T | null) => void;
	options: Array<SelectOption<T>>;
	allowNone?: boolean;
	noneLabel?: string;
	firstOption?: SelectOption<T>;
	helperText?: string;
	fullWidth?: boolean;
	margin?: "none" | "dense" | "normal";
	info?: FieldInfo;
}

function FormSelectField<T extends string | number>({
	label,
	value,
	onChange,
	options,
	allowNone = true,
	noneLabel = "None",
	firstOption,
	helperText,
	fullWidth = true,
	margin = "normal",
}: Readonly<FormSelectFieldProps<T>>): React.ReactElement {
	const labelId = useId();
	// With an entry above None, None is a choice of its own and has to stay readable when picked.
	const showsNone = firstOption !== undefined;

	const handleChange = (e: SelectChangeEvent<T | "">) => {
		const selectedValue = e.target.value;
		onChange(selectedValue === "" ? null : (selectedValue as T));
	};

	return (
		<FormControl fullWidth={fullWidth} margin={margin}>
			<InputLabel id={labelId} shrink={showsNone || undefined}>
				{label}
			</InputLabel>
			<Select<T | "">
				labelId={labelId}
				value={value}
				label={label}
				displayEmpty={showsNone}
				notched={showsNone || undefined}
				onChange={handleChange}
			>
				{firstOption && (
					<MenuItem value={firstOption.id}>{firstOption.label}</MenuItem>
				)}
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
			{helperText && <FormHelperText>{helperText}</FormHelperText>}
		</FormControl>
	);
}

export default FormSelectField;
