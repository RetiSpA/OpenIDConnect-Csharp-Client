﻿using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using FluentAssertions;
using OpenIDClient;
using SimpleWebServer;
using JWT;
using Griffin.WebServer;
using Griffin.WebServer.Modules;

namespace OIDC.Tests
{
    [TestFixture]
    public class DynamicClientRegistrationTests : OIDCTests
    {
        WebServer ws;

        [SetUp]
        public void StartWebServer()
        {
            X509Certificate2 certificate = new X509Certificate2("certificate.crt", "");
            ws = new WebServer(myBaseUrl.ToString(), certificate);
            ws.addUrlAction("/my_public_keys.jwks", RespondWithJwks);
            ws.Run();
        }

        [TearDown]
        public void Cleanup()
        {
            ws.Stop();
        }

        private void RespondWithJwks(IHttpContext context)
        {
            X509Certificate signCert = new X509Certificate();
            signCert.Import("server.crt");
            X509Certificate encCert = new X509Certificate();
            encCert.Import("server.crt");

            Dictionary<string, object> keysDict = OpenIdRelyingParty.GetKeysJwks(signCert, encCert);

            IJsonSerializer JsonSerializer = new DefaultJsonSerializer();
            string rstring = JsonSerializer.Serialize(keysDict);
            HttpWorker.WriteTextToResponse(context, rstring);
        }

        /// <summary>
        /// Uses dynamic registration
        /// 
        /// Description:	
        /// Use the client registration endpoint in order to dynamically register the Relying Party.
        /// Expected result:	
        /// Get a Client Registration Response.
        /// </summary>
        [TestCase]
        public void Should_Client_Be_Able_To_Register()
        {
            // given
            rpid = "rp-registration-dynamic";
            claims = "normal";

            string registrationEndopoint = GetBaseUrl("/registration");
            OIDCClientInformation clientMetadata = new OIDCClientInformation();
            clientMetadata.ApplicationType = "web";
            clientMetadata.RedirectUris = new List<string> { myBaseUrl + "code_flow_callback" };
            clientMetadata.ResponseTypes = new List<string> { "code" };
            OpenIdRelyingParty rp = new OpenIdRelyingParty();

            // when
            OIDCClientInformation response = rp.RegisterClient(registrationEndopoint, clientMetadata);

            // then
            response.validate();
        }

        /// <summary>
        /// Registration request has redirect_uris
        /// 
        /// Description:	
        /// Set the redirect_uris parameter of the Client Metadata in a registration request.
        /// Expected result:	
        /// Get a Client Registration Response.
        /// </summary>
        [TestCase]
        public void Should_Registration_Request_Has_RedirectUris()
        {
            // given
            rpid = "rp-registration-redirect_uris";
            claims = "normal";

            string registrationEndopoint = GetBaseUrl("/registration");
            OIDCClientInformation clientMetadata = new OIDCClientInformation();
            clientMetadata.ApplicationType = "web";
            clientMetadata.RedirectUris = new List<string> { myBaseUrl + "code_flow_callback" };
            clientMetadata.ResponseTypes = new List<string> { "code" };
            OpenIdRelyingParty rp = new OpenIdRelyingParty();

            // when
            OIDCClientInformation response = rp.RegisterClient(registrationEndopoint, clientMetadata);

            // then
            response.validate();
            CollectionAssert.AreEquivalent(clientMetadata.RedirectUris, response.RedirectUris);
        }

        /// <summary>
        /// Keys are published as a well-formed JWK Set
        /// 
        /// Description:	
        /// The keys published by the Relying Party should follow the JSON Web Key Set (JWK Set) Format.
        /// Expected result:	
        /// Get a Client Registration Response.
        /// </summary>
        [TestCase]
        public void Should_Keys_Be_Published_As_JWK()
        {
            // given
            rpid = "rp-registration-well_formed_jwk";
            claims = "normal";

            string registrationEndopoint = GetBaseUrl("/registration");
            OIDCClientInformation clientMetadata = new OIDCClientInformation();
            clientMetadata.ApplicationType = "web";
            clientMetadata.RedirectUris = new List<string> { myBaseUrl + "code_flow_callback" };
            clientMetadata.ResponseTypes = new List<string> { "code" };
            clientMetadata.JwksUri = myBaseUrl + "my_public_keys.jwks";
            OpenIdRelyingParty rp = new OpenIdRelyingParty();

            // when
            OIDCClientInformation response = rp.RegisterClient(registrationEndopoint, clientMetadata);

            // then
            response.validate();
        }

        /// <summary>
        /// Uses HTTPS for all endpoints
        /// 
        /// Description:	
        /// Only register URLs using the https scheme for all endpoints in the Client Metadata.
        /// Expected result:	
        /// No endpoints not supporting HTTPS.
        /// </summary>
        [TestCase]
        [ExpectedException(typeof(OIDCException), ExpectedMessage = "Some of the URIs for the client is not on https")]
        public void Should_Client_Only_Use_Https_Endpoints()
        {
            // given
            rpid = "rp-registration-uses_https_endpoints";
            claims = "normal";

            string registrationEndopoint = GetBaseUrl("/registration");
            OIDCClientInformation clientMetadata = new OIDCClientInformation();
            clientMetadata.ApplicationType = "web";
            clientMetadata.RedirectUris = new List<string> { myBaseUrl + "code_flow_callback" };
            clientMetadata.ResponseTypes = new List<string> { "code" };
            clientMetadata.JwksUri = myBaseUrl + "my_public_keys.jwks";
            OpenIdRelyingParty rp = new OpenIdRelyingParty();

            // when
            OIDCClientInformation response = rp.RegisterClient(registrationEndopoint, clientMetadata);
            response.JwksUri = clientMetadata.JwksUri.Replace("https", "http");
            // then
            response.validate();
        }
    }
}