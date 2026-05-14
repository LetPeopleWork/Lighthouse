import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { IAuthenticationMethod } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import AuthMethodDropdown from "./AuthMethodDropdown";

const patMethod: IAuthenticationMethod = {
	key: "jira.cloud",
	displayName: "Jira Cloud",
	options: [
		{
			key: "ApiToken",
			displayName: "API Token",
			isSecret: true,
			isOptional: false,
		},
	],
};

const oauthMethod: IAuthenticationMethod = {
	key: "jira.oauth",
	displayName: "OAuth 2.0",
	options: [],
	isPremium: true,
};

const openDropdown = async (user: ReturnType<typeof userEvent.setup>) => {
	const select = screen.getByRole("combobox");
	await user.click(select);
};

const renderDropdown = (props: {
	canUsePremiumFeatures: boolean;
	selectedKey: string;
	methods: IAuthenticationMethod[];
	onChange?: (key: string) => void;
}) =>
	render(
		<AuthMethodDropdown
			methods={props.methods}
			selectedKey={props.selectedKey}
			canUsePremiumFeatures={props.canUsePremiumFeatures}
			onChange={props.onChange ?? (() => undefined)}
		/>,
	);

describe("AuthMethodDropdown", () => {
	it("renders premium option without disabled styling when user has premium", async () => {
		const user = userEvent.setup();
		renderDropdown({
			canUsePremiumFeatures: true,
			selectedKey: patMethod.key,
			methods: [patMethod, oauthMethod],
		});

		await openDropdown(user);

		const listbox = await screen.findByRole("listbox");
		const oauthOption = within(listbox).getByRole("option", {
			name: /OAuth 2\.0/,
		});
		expect(oauthOption).not.toHaveAttribute("aria-disabled", "true");
		expect(oauthOption.textContent).not.toMatch(/\(Premium\)/i);
	});

	it("renders premium option with disabled styling AND a (Premium) suffix when user lacks premium", async () => {
		const user = userEvent.setup();
		renderDropdown({
			canUsePremiumFeatures: false,
			selectedKey: patMethod.key,
			methods: [patMethod, oauthMethod],
		});

		await openDropdown(user);

		const listbox = await screen.findByRole("listbox");
		const oauthOption = within(listbox).getByRole("option", {
			name: /OAuth 2\.0/,
		});
		expect(oauthOption.textContent).toMatch(/\(Premium\)/i);
		expect(oauthOption.getAttribute("aria-disabled")).toBe("true");
	});

	it("renders non-premium options without (Premium) suffix regardless of license state", async () => {
		const user = userEvent.setup();
		renderDropdown({
			canUsePremiumFeatures: false,
			selectedKey: patMethod.key,
			methods: [patMethod, oauthMethod],
		});

		await openDropdown(user);

		const listbox = await screen.findByRole("listbox");
		const patOption = within(listbox).getByRole("option", {
			name: /Jira Cloud/,
		});
		expect(patOption.textContent).not.toMatch(/\(Premium\)/i);
		expect(patOption).not.toHaveAttribute("aria-disabled", "true");
	});

	it("invokes onChange with the selected key when the user picks an option", async () => {
		const user = userEvent.setup();
		const handleChange = vi.fn();
		renderDropdown({
			canUsePremiumFeatures: true,
			selectedKey: patMethod.key,
			methods: [patMethod, oauthMethod],
			onChange: handleChange,
		});

		await openDropdown(user);
		const listbox = await screen.findByRole("listbox");
		await user.click(
			within(listbox).getByRole("option", { name: /OAuth 2\.0/ }),
		);

		expect(handleChange).toHaveBeenCalledWith("jira.oauth");
	});
});
