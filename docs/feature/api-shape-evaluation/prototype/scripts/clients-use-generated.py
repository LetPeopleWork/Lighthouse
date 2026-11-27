"""SCRATCH COPY ONLY: makes the pre-fix client read GET /v1/deliveries/portfolio/{id} through the
generated answer type instead of its hand-written `readonly unknown[]` cast. Nothing else changes."""
import sys

path = sys.argv[1]
text = open(path).read()

def swap(old, new, count=1):
    global text
    assert text.count(old) == count, (old, text.count(old))
    text = text.replace(old, new)

swap(
    "  readonly listDeliveries: (\n    portfolioId: number,\n  ) => Promise<LighthouseApiResult<readonly unknown[]>>;",
    "  readonly listDeliveries: (\n    portfolioId: number,\n  ) => Promise<LighthouseApiResult<PortfolioDeliveriesAnswer>>;",
)
swap(
    "    listDeliveries: async (portfolioId: number) =>\n      requestJson<readonly unknown[]>(",
    "    listDeliveries: async (portfolioId: number) =>\n      requestJson<PortfolioDeliveriesAnswer>(",
)
header = (
    'import type { getApiV1DeliveriesPortfolioPortfolioIdResponse200ApplicationJson } from "./generated/api/deliveries/deliveries";\n'
    "\n"
    "/** The answer of GET /v1/deliveries/portfolio/{id}, as the server's OpenAPI document states it. */\n"
    'type PortfolioDeliveriesAnswer = getApiV1DeliveriesPortfolioPortfolioIdResponse200ApplicationJson["data"];\n'
)
text = header + text
open(path, "w").write(text)
print("patched", path)
