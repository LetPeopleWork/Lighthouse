{{/* Chart name + fullname */}}
{{- define "lighthouse.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "lighthouse.fullname" -}}
{{- printf "%s-%s" .Release.Name (include "lighthouse.name" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/* Common labels */}}
{{- define "lighthouse.labels" -}}
app.kubernetes.io/name: {{ include "lighthouse.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/version: {{ include "lighthouse.imageTag" . | quote }}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version }}
{{- end -}}

{{/* API selector labels */}}
{{- define "lighthouse.api.selectorLabels" -}}
app.kubernetes.io/name: {{ include "lighthouse.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: api
{{- end -}}

{{/* Postgres selector labels */}}
{{- define "lighthouse.postgres.selectorLabels" -}}
app.kubernetes.io/name: {{ include "lighthouse.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: postgres
{{- end -}}

{{/* Image tag: values.image.tag wins, else Chart.appVersion (ADR-083 consistency) */}}
{{- define "lighthouse.imageTag" -}}
{{- .Values.image.tag | default .Chart.AppVersion -}}
{{- end -}}

{{/* Bundled Postgres service host */}}
{{- define "lighthouse.postgres.host" -}}
{{- printf "%s-postgres" (include "lighthouse.fullname" .) -}}
{{- end -}}

{{/* Secret name holding DB connection string + Postgres password */}}
{{- define "lighthouse.secretName" -}}
{{- printf "%s-db" (include "lighthouse.fullname" .) -}}
{{- end -}}

{{/* DB Secret name — a pre-existing Secret (postgresql.auth.existingSecret) or the chart-owned one.
     When set, an operator (slice-03 hand-made) or an external secret store (ESO/OpenBao, slice-04)
     owns the credential, so the password is never a Helm value (CC-3). The Secret MUST provide both
     keys the chart consumes: Database__ConnectionString and postgres-password. */}}
{{- define "lighthouse.db.secretName" -}}
{{- if .Values.postgresql.auth.existingSecret -}}
{{- .Values.postgresql.auth.existingSecret -}}
{{- else -}}
{{- include "lighthouse.secretName" . -}}
{{- end -}}
{{- end -}}

{{/* Whether the chart renders the DB keys into its own Secret. False when a pre-existing Secret
     supplies them (postgresql.auth.existingSecret) — then the render-time password `required`
     (ADR-082) is relaxed, since the credential lives outside the chart. */}}
{{- define "lighthouse.renderDbKeys" -}}
{{- if .Values.postgresql.auth.existingSecret -}}
false
{{- else -}}
true
{{- end -}}
{{- end -}}

{{/* Effective DB host — bundled Postgres service or externalDatabase.host (for the startup wait) */}}
{{- define "lighthouse.db.host" -}}
{{- if .Values.postgresql.enabled -}}
{{- include "lighthouse.postgres.host" . -}}
{{- else -}}
{{- .Values.externalDatabase.host -}}
{{- end -}}
{{- end -}}

{{/* Effective DB port — bundled Postgres is 5432, else externalDatabase.port (default 5432) */}}
{{- define "lighthouse.db.port" -}}
{{- if .Values.postgresql.enabled -}}
5432
{{- else -}}
{{- .Values.externalDatabase.port | default 5432 -}}
{{- end -}}
{{- end -}}

{{/* DB-mode guard — exactly one of bundled Postgres or externalDatabase (ADR-080/082) */}}
{{- define "lighthouse.assertDatabase" -}}
{{- $ext := .Values.externalDatabase | default dict -}}
{{- if and .Values.postgresql.enabled $ext.host -}}
{{- fail "ambiguous database config: set EITHER postgresql.enabled=true (bundled) OR externalDatabase.* (BYO), not both" -}}
{{- else if not (or .Values.postgresql.enabled $ext.host) -}}
{{- fail "no database configured: enable bundled Postgres (postgresql.enabled=true) or set externalDatabase.host (BYO)" -}}
{{- end -}}
{{- end -}}

{{/* Npgsql connection string — bundled Postgres or externalDatabase (contains the password → Secret) */}}
{{- define "lighthouse.connectionString" -}}
{{- include "lighthouse.assertDatabase" . -}}
{{- if .Values.postgresql.enabled -}}
{{- $a := .Values.postgresql.auth -}}
{{- printf "Host=%s;Port=5432;Database=%s;Username=%s;Password=%s" (include "lighthouse.postgres.host" .) $a.database $a.username (required "postgresql.auth.password is required (ADR-082): set a Postgres password" $a.password) -}}
{{- else -}}
{{- $e := .Values.externalDatabase -}}
{{- printf "Host=%s;Port=%d;Database=%s;Username=%s;Password=%s" $e.host (int (default 5432 $e.port)) (required "externalDatabase.database is required" $e.database) (required "externalDatabase.user is required" $e.user) (required "externalDatabase.password is required" $e.password) -}}
{{- end -}}
{{- end -}}

{{/* MCP guard — mcp.enabled needs an image */}}
{{- define "lighthouse.assertMcp" -}}
{{- if .Values.mcp.enabled -}}
{{- if not .Values.mcp.image -}}
{{- fail "mcp.enabled=true requires mcp.image (the lighthouse-clients mcp-http image)" -}}
{{- end -}}
{{- end -}}
{{- end -}}

{{/* MCP service host */}}
{{- define "lighthouse.mcp.host" -}}
{{- printf "%s-mcp" (include "lighthouse.fullname" .) -}}
{{- end -}}

{{/* Resolved public access URL for NOTES.txt */}}
{{- define "lighthouse.accessURL" -}}
{{- $scheme := ternary "https" "http" .Values.ingress.tls -}}
{{- printf "%s://%s" $scheme .Values.ingress.host -}}
{{- end -}}

{{/* Scaling guard — replicaCount>1 needs the Redis backplane (epic-5305 #5304) or pods double-sync */}}
{{- define "lighthouse.assertScaling" -}}
{{- if and (gt (int .Values.replicaCount) 1) (not .Values.redis.connectionString) -}}
{{- fail "replicaCount>1 requires redis.connectionString (the epic-5305 SignalR backplane / single-instance background work); set it or scale to 1" -}}
{{- end -}}
{{- end -}}

{{/* frontend.mode guard — embedded is supported; split fails loud (ADR-081, Band D deferred) */}}
{{- define "lighthouse.assertFrontendMode" -}}
{{- if eq .Values.frontend.mode "split" -}}
{{- fail "frontend.mode=split is not implemented in this chart version; use frontend.mode=embedded (ADR-081)" -}}
{{- else if ne .Values.frontend.mode "embedded" -}}
{{- fail (printf "frontend.mode must be 'embedded' (got %q)" .Values.frontend.mode) -}}
{{- end -}}
{{- end -}}
