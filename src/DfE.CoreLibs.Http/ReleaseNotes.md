# 2.0.2
Fixed passing in an empty GUID in the x-correlationId header causing an exception. Now if an empty GUID is detected a bad request will be returned.
If an empty guid is returned, the content of the response returned (along with the 400 status code) will be 

`{ StatusCode = 400, Message = Bad Request. x-correlationId header cannot be an empty GUID }`

# 2.0.1
Fix to the log output so that `x-correlationId [guid]` is now `x-correlationId: [guid]

# 2.0.0
Introduced logging scope output.
Changed to only support GUID correlation ids (breaking changes)

# 1.0.0
Initial version