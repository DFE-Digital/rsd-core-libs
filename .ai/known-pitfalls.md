# Known pitfalls (CoreLibs)

1. Assembly version `1.0.0.0` locally vs published `1.0.x.0`.
2. `DistributedCacheAdapter.PropagateReadErrors` default **false** — session must use `SessionDistributedCacheAdapter`.
3. Redis/Service Bus connection resolution throws if neither ConnectionStrings nor nested settings exist.
4. `AddDfEMassTransit` custom Azure configurator skips `ConfigureEndpoints`.
5. FlexForms registers custom `IEventPublisher` **after** MassTransit.
6. Empty package readmes (Contracts, Http, Messaging).
7. FileStorage shipping test dependencies.
8. Authorization policies treating scopes like roles (Security readme).
9. Application Insights 3.x vs Serilog is a **consumer** issue (FlexForms pins 2.x).
10. `OpenIdConnect` SignedOutCallbackPath historically not assigned in an extension — verify before relying on it for DfE Sign-In logout.
