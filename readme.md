# CDR API

## Design

The fundamental design decision is the choice of persistent storage. A Call Detail Record (CDR) is a time series with a fixed structure, so Azure Data Explorer (ADX) appears to be a suitable option.

1. **Chunked CSV Ingestion**

   * The input CSV file is split into smaller chunks.
   * Each chunk is compressed and sent in parallel to ADX using queued ingest, eliminating the need for custom retry logic.
   * Memory usage remains approximately 350 MB regardless of input file size.
   * CSV chunk size and parallelism are configurable via `CallDetailsOptions`.

2. **Schema Adjustments & Validation**

   * Input data structure is adjusted to match ADX-supported types (e.g., merging `call_date` and `end_time`).
   * Transformation logic is simple and sensitive to column order; header validation ensures correct schema before ingestion.
   * Deduplication on retry is based on the input file name.

3. **ADX Configuration**

   * ADX settings (connection strings, retry policies, etc.) are specified in `AdxCallDetailRepositoryOptions`.
   * Solution assumes the existence of a Kusto table with the following structure:


   ```kusto
   .create table CallDetails (
   caller_id: long, 
   recipient: long, 
   call_end_datetime: datetime, 
   duration: int, 
   cost: real, 
   reference: string, 
   currency: string
   )
   ```

Current config points to preconfigured ADX cluster available for testing. You should be able to run majority of integration tests in the solution, except tests, that require large pre-generated input .csv files (these csv files are not part of repo).

## Project Structure

* The project is divided into four main traditional namespaces following clean architecture principles: Domain, Application, WebAPI, and Infrastructure. All layers remain in a one project, with dependencies only pointing inward. Splitting into separate projects per layer seems to me like overkill in this simple case.&#x20;

* **Domain**: Defines the [CallDetail](Giacom.Cdr.Api/Domain/Entities/CallDetail.cs) entity.

* **Application**: Implements use cases [SplitCallDetailsCsvHandler](Giacom.Cdr.Api/Application/Handlers/SplitCallDetailsCsvHandler.cs), [UploadCallDetailsHandler](Giacom.Cdr.Api/Application/Handlers/UploadCallDetailsHandler.cs),  [QueryCallDetailsHandler](Giacom.Cdr.Api/Application/Handlers/QueryCallDetailsHandler.cs) and their options in (CallDetailsOptions)[Giacom.Cdr.Api/Application/CallDetailsOptions.cs]

* **WebAPI**: Exposes [upload](Giacom.Cdr.Api/WebAPI/Controllers/CallDetailsController.cs#L39) and [query](Giacom.Cdr.Api/WebAPI/Controllers/CallDetailsController.cs#L57) endpoints. Although the requirements didn't specify any read operations, I added a query endpoint to allow verifying that the data was successfully uploaded. In a real scenario, the read API would likely be separated into its own service.

* **Infrastructure**:  ADX repository ingestion in [AdxCallDetailRepository](Giacom.Cdr.Api/Infrastructure/Repository/AdxClassDetailRepository.cs) via `Microsoft.Azure.Kusto.*` libraries and ADX related options in [AdxCallDetailRepositoryOptions](Giacom.Cdr.Api/Infrastructure/Repository/AdxCallDetailRepositoryOptions.cs)

**Additional details:**

* In-process messaging uses the **Wolverine** library (more ambitious and minimal boilerplate compared to **MediatR**).
* Mapping is done with **Mapster** for performance and flexibility (faster and more extensible than **AutoMapper**).
* Basic diagnostics are provided by [DiagnosticsMiddleware](Giacom.Cdr.Api/Application/Common/Wolverine/DiagnosticsMiddleware.cs).

## Tests

* **Integration Tests**:

  * Verify uploading of pre-generated data (for large volume tests).
  * Verify querying of pre-ingested data.
  * Verify uploading of runtime-generated data with subsequent verification of the correct structure of inserted data via the query endpoint.
  * Some tests use fake implementation of CDR repository - [FakeCallDetailReposiory][(Giacom.Cdr.IntegrationTests/FakeCallDetailRepository.cs)
* **Unit Tests**:

  * Validate CSV splitting and validation logic
* **Performance Test**:

  * Simple unit test measures CSV split duration using **NBench** library

## N2H / Space for Improvements

* Remove sensitive data from configuration.
* Enable integration tests to create and tear down ADX resources per run.
* Integration test for deduplication on ADX level (e.g. ingest same file twice)
* The upload endpoint uses multipart/form-data, which is not the recommended method for uploading large files; however, I believe that if there is no parallel upload of a large number of files, it is still usable. An alternative is streaming directly from the HTTP body.
* Checkpointing: If an upload needs to be retried completely, all chunks are resent (deduplication occurs only in ADX). It would be nice to store successfully sent CSV chunks in a simple separate storage (e.g., Azure Table Storage) and on subsequent uploads ingest only the missing data.
* CSV splitting and sending CSV chunks could run partially in parallel. The benefit probably wouldn't be significant, since CSV splitting is many times faster than the upload itself, but it primarily depends on upload bandwidth, which in my DEV environment is not particularly impressive. However, as uploads occur only once daily, the cost-to-performance ratio of this optimization doesn't seem favorable; I’d stick to the golden rule: "Premature optimization is the root of all evil."
* Theoretically, we wouldn’t need to save CSV chunks back to disk; however, as in the previous case, the potential performance impact doesn’t seem significant to me.
* Implement query-level deduplication using ADX materialized views.
