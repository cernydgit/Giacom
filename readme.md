# CDR Details API

## Design

The fundamental design decision is the choice of persistent storage. A Call Detail Record (CDR) is a time series with a fixed structure, so Azure Data Explorer (ADX) appears to be a suitable option.

1. **Chunked CSV Ingestion**

   * The input CSV file is split into smaller chunks with the same schema.
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
     caller_id: string,
     recipient: string,
     call_date: datetime,
     end_time: datetime,
     duration: long,
     cost: real,
     reference: string,
     currency: string
   )
   ```

Current config points to preconfigured ADX cluster available for testing.

## Project Structure

* The project is divided into four main traditional namespaces following clean architecture principles: Domain, Application, WebAPI, and Infrastructure. All layers remain in a one project, with dependencies only pointing inward. Splitting into separate projects per layer seems to me like overkill in this simple case.&#x20;

* **Domain**: Defines the `CallDetail` entity.

* **Application**: Implements use cases (upload, split, query) and their options in `CallDetailsOptions`

* **WebAPI**: Exposes upload and query endpoints. Although the requirements didn't specify any read operations, I added a query endpoint to allow verifying that the data was successfully uploaded. In a real scenario, the read API would likely be separated into its own service.

* **Infrastructure**:  ADX repository ingestion via `Microsoft.Azure.Kusto.*` libraries and ADX related options 

**Additional details:**

* In-process messaging uses the **Wolverine** library (more ambitious and minimal boilerplate compared to **MediatR**).
* Mapping is done with **Mapster** for performance and flexibility (faster and more extensible than AutoMapper).
* Basic diagnostics are provided by `DiagnosticsMiddleware`.

## Tests

* **Integration Tests**:

  * Verify uploading of pre-generated data (for large volume tests).
  * Verify querying of pre-ingested data.
  * Verify uploading of runtime-generated data with subsequent verification of the correct structure of inserted data via the query endpoint.
* **Unit Tests**:

  * Validate CSV splitting and validation logic
* **Performance Test**:

  * Simple unit test measures CSV split duration using **NBench** library

## N2H / Space for Improvements

* Remove sensitive data from configuration.
* Enable integration tests to create and tear down ADX resources per run.
* The upload endpoint uses multipart/form-data, which is not the recommended method for uploading large files; however, I believe that if there is no parallel upload of a large number of files, it is still usable. An alternative is streaming directly from the HTTP body.
* Checkpointing: If an upload needs to be retried completely, all chunks are resent (deduplication occurs only in ADX). It would be nice to store successfully sent CSV chunks in a simple separate storage (e.g., Azure Table Storage) and on subsequent uploads ingest only the missing data.
* CSV splitting and sending CSV chunks could run partially in parallel. The benefit probably wouldn't be significant, since CSV splitting is many times faster than the upload itself, but it primarily depends on upload bandwidth, which in my DEV environment is not particularly impressive. However, as uploads occur only once daily, the cost-to-performance ratio of this optimization doesn't seem favorable; I’d stick to the golden rule: "Premature optimization is the root of all evil."
* Theoretically, we wouldn’t need to save CSV chunks back to disk; however, as in the previous case, the potential performance impact doesn’t seem significant to me.
* Implement query-level deduplication using ADX materialized views.
