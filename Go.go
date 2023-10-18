package main

import (
	"crypto/hmac"
	"crypto/sha256"
	b64 "encoding/base64"
	"encoding/hex"
	"fmt"
	"strings"
)

func main() {
	// create a random 64 bytes (512 bits) secret
	secret := []byte("JKQby5i3bC4vr8PS0Rp7gk52vV6IBa")

	data := []byte(strings.Join([]string{"GET", "/rest/sites/workinghours/urgent%20si", "2021-03-10T14:23:23Z"}, "+"))

	// create a new HMAC by defining the hash type and the key
	hmac := hmac.New(sha256.New, secret)

	// compute the HMAC
	hmac.Write([]byte(data))
	dataHmac := hmac.Sum(nil)

	secretHex := hex.EncodeToString(secret)

	fmt.Printf("HMAC_SHA256(key: %s, data: %s): %s", secretHex, string(data), dataHmac)

	sEnc := b64.StdEncoding.EncodeToString([]byte(dataHmac))
	fmt.Println("\nbase64 encoded text: ", sEnc)
}
