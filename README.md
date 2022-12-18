# Grpc-File-Service
The gRpc file service is written to act as a file server on Kubernetes cluster. The files are persisted in hostPath in case of local cluster. 
When working with AKS or EKS an Azure disk or EFS coulc be used. The service using gRPC streaming. Sending of files in chunk is built into this. 
Apart from this, to save space, Compression can be enabled in the service. The compression that is used is Brotoli compression. 
The service does not have json transcoding endpoint as Json transcoding does not support gRPC streaming. 
To expose this servie outside the cluster the load balancer should have HTTP/2 enabled. The the endpoint should be protected via SSL. 
The service can still be a clusterip service and Ingress can be used to redirect the request from external call to the service on  the cluster.
