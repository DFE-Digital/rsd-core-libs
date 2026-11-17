# Known pitfalls (CoreLibs)

1. Assembly version `1.0.0.0` locally vs published `1.0.x.0`.
2. `DistributedCacheAdapter.PropagateReadErrors` default **false** — session must use `SessionDistributedCacheAdapter`.
3. Redis/Service Bus connection resolution throws if neither ConnectionStrings nor nested settings exist.
4. `AddDfEMassTransit` custom Azure configurator skips `ConfigureEndpoints`.
5. FlexForms registers custom `IEventPublisher` **after** MassTransit.
6. Empty package readmes (Contracts, Http, Messaging).
7. FileStorage shipping test dependencies.
8. Authorization policies must distinguish scopes from roles (see the Security readme).
9. Application Insights 3.x vs Serilog is a **consumer** issue (FlexForms pins 2.x).
10. When relying on DfE Sign-In logout through `AddCustomOpenIdConnect`, confirm `SignedOutCallbackPath` is configured.
