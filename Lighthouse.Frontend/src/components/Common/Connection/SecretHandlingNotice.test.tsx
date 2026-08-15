import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import SecretHandlingNotice, {
	SECRET_HANDLING_DOCS_URL,
	SECRET_HANDLING_NOTICE_TEST_ID,
} from "./SecretHandlingNotice";

const noticeText = (): string =>
	screen.getByTestId(SECRET_HANDLING_NOTICE_TEST_ID).textContent ?? "";

describe("SecretHandlingNotice", () => {
	describe("What it promises", () => {
		it("says the credential is encrypted before it is saved", () => {
			render(<SecretHandlingNotice />);
			expect(noticeText()).toMatch(/encrypted before .*saved/i);
		});

		it("says the credential is never shown again to anyone", () => {
			render(<SecretHandlingNotice />);
			expect(noticeText()).toMatch(/never shown again/i);
			expect(noticeText()).toMatch(/not even to an administrator/i);
		});

		it("says the credential never leaves this instance", () => {
			render(<SecretHandlingNotice />);
			expect(noticeText()).toMatch(/never leave this instance/i);
		});

		it("says the credential can be revoked where it was created", () => {
			render(<SecretHandlingNotice />);
			expect(noticeText()).toMatch(/revoke .*wherever you created it/i);
		});
	});

	describe("What it deliberately does not promise", () => {
		it("makes no claim about which encryption key this instance holds", () => {
			render(<SecretHandlingNotice />);
			expect(noticeText()).not.toMatch(/\bkeys?\b/i);
		});

		it("makes no claim of protection against someone holding the key or the host", () => {
			render(<SecretHandlingNotice />);
			expect(noticeText()).not.toMatch(
				/\b(attacker|intruder|breach|stolen|server access|host)\b/i,
			);
		});

		it("names no way of protecting the credential beyond the fact it is encrypted", () => {
			render(<SecretHandlingNotice />);
			expect(noticeText()).not.toMatch(
				/\b(aes|rsa|gcm|cbc|sha-?\d*|hmac|pbkdf2|envelope|cipher|algorithm)\b/i,
			);
		});

		it("names no work tracking system", () => {
			render(<SecretHandlingNotice />);
			expect(noticeText()).not.toMatch(
				/\b(azure\s*devops|jira|linear|servicenow|csv|github|trello)\b/i,
			);
		});
	});

	describe("How it is presented", () => {
		it("answers a question rather than raising an alarm", () => {
			const { container } = render(<SecretHandlingNotice />);
			expect(screen.queryByRole("alert")).not.toBeInTheDocument();
			expect(container.querySelector(".MuiAlert-root")).toBeNull();
			expect(noticeText()).not.toMatch(/\b(warning|caution|danger|risk)\b/i);
		});

		it("links to the published explanation of how Lighthouse handles credentials", () => {
			render(<SecretHandlingNotice />);
			const link = screen.getByRole("link");
			expect(link).toHaveAttribute("href", SECRET_HANDLING_DOCS_URL);
			expect(link.textContent).toMatch(/protects your credentials/i);
		});

		it("keeps the sentence and the link from running into each other", () => {
			render(<SecretHandlingNotice />);
			expect(noticeText()).toMatch(/immediately\.\s+How Lighthouse protects/);
		});

		it("lets a host repoint the link once a dedicated page exists", () => {
			render(<SecretHandlingNotice docsUrl="https://example.test/security" />);
			expect(screen.getByRole("link")).toHaveAttribute(
				"href",
				"https://example.test/security",
			);
		});
	});
});
