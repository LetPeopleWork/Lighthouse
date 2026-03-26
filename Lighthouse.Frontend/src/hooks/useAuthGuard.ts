import { useCallback, useEffect, useRef, useState } from "react";
import {
	AuthMode,
	type AuthSessionStatus,
	type RuntimeAuthStatus,
} from "../models/Auth/AuthModels";
import type { IAuthService } from "../services/Api/AuthService";

export type AuthShell =
	| "loading"
	| "anonymous"
	| "login"
	| "authenticated"
	| "misconfigured"
	| "session-expired";

export interface AuthGuardState {
	shell: AuthShell;
	loginUrl: string;
	misconfigurationMessage?: string;
	session?: AuthSessionStatus;
	runtimeStatus?: RuntimeAuthStatus;
	logout: () => Promise<void>;
}

const SESSION_CHECK_INTERVAL_MS = 60_000;

export function useAuthGuard(authService: IAuthService): AuthGuardState {
	const [shell, setShell] = useState<AuthShell>("loading");
	const [runtimeStatus, setRuntimeStatus] = useState<RuntimeAuthStatus>();
	const [session, setSession] = useState<AuthSessionStatus>();
	const [misconfigurationMessage, setMisconfigurationMessage] =
		useState<string>();
	const wasAuthenticated = useRef(false);
	const sessionCheckTimer = useRef<ReturnType<typeof setInterval> | undefined>(
		undefined,
	);

	const loginUrl = authService.getLoginUrl();

	const checkSession = useCallback(async () => {
		try {
			const sessionStatus = await authService.getSession();
			setSession(sessionStatus);

			if (sessionStatus.isAuthenticated) {
				wasAuthenticated.current = true;
				setShell("authenticated");
			} else if (wasAuthenticated.current) {
				setShell("session-expired");
				if (sessionCheckTimer.current) {
					clearInterval(sessionCheckTimer.current);
					sessionCheckTimer.current = undefined;
				}
			} else {
				setShell("login");
			}
		} catch {
			if (wasAuthenticated.current) {
				setShell("session-expired");
			} else {
				setShell("login");
			}
		}
	}, [authService]);

	useEffect(() => {
		let cancelled = false;

		const bootstrap = async () => {
			try {
				const status = await authService.getRuntimeAuthStatus();
				if (cancelled) return;
				setRuntimeStatus(status);

				switch (status.mode) {
					case AuthMode.Disabled:
						setShell("anonymous");
						break;
					case AuthMode.Misconfigured:
						setMisconfigurationMessage(status.misconfigurationMessage);
						setShell("misconfigured");
						break;
					case AuthMode.Enabled:
					case AuthMode.Blocked:
						await checkSession();
						if (!cancelled) {
							sessionCheckTimer.current = setInterval(
								checkSession,
								SESSION_CHECK_INTERVAL_MS,
							);
						}
						break;
				}
			} catch {
				if (!cancelled) {
					setShell("anonymous");
				}
			}
		};

		bootstrap();

		return () => {
			cancelled = true;
			if (sessionCheckTimer.current) {
				clearInterval(sessionCheckTimer.current);
			}
		};
	}, [authService, checkSession]);

	const logout = useCallback(async () => {
		try {
			wasAuthenticated.current = false;
			if (sessionCheckTimer.current) {
				clearInterval(sessionCheckTimer.current);
				sessionCheckTimer.current = undefined;
			}

			// For OIDC logout, we need the browser to follow the redirect chain.
			// POST to /api/auth/logout returns a redirect to the IdP's end_session endpoint.
			// Use a form submission to let the browser handle redirects with cookies.
			const form = document.createElement("form");
			form.method = "POST";
			form.action = loginUrl.replace("/login", "/logout");
			document.body.appendChild(form);
			form.submit();
		} catch {
			globalThis.location.href = "/";
		}
	}, [loginUrl]);

	return {
		shell,
		loginUrl,
		misconfigurationMessage,
		session,
		runtimeStatus,
		logout,
	};
}
