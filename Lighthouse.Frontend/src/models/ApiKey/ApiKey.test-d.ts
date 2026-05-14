import type { IApiKeyCreationResult, IApiKeyInfo } from "./ApiKey";

// Regression guard: if these compile-time checks fail, `createdByUser` was reintroduced on a public API DTO interface — see DISTILL feature-delta.md 'Test-factory regression guard'.

type _AssertNoCreatedByUserOnInfo = "createdByUser" extends keyof IApiKeyInfo
	? never
	: true;
type _AssertNoCreatedByUserOnResult =
	"createdByUser" extends keyof IApiKeyCreationResult ? never : true;

const _noCreatedByUserOnInfo: _AssertNoCreatedByUserOnInfo = true;
const _noCreatedByUserOnResult: _AssertNoCreatedByUserOnResult = true;

export { _noCreatedByUserOnInfo, _noCreatedByUserOnResult };
