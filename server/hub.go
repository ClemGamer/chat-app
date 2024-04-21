package main

import (
	"encoding/json"
	"fmt"
)

// Hub maintains the active clients and broadcast
// messages to the clients.
type Hub struct {
	// Registered clients.
	clients map[int]*Client

	// Register requests from the clients.
	register chan *Client

	// Unregister requests from the Clients.
	unregister chan *Client

	// Inbound messages from the clients.
	broadcast chan *WrapMessage
}

type WrapMessage struct {
	Protocal string      `json:"protocal"`
	ID       int         `json:"id"`
	Data     interface{} `json:"data"`
}

func (m *WrapMessage) Marshal() ([]byte, error) {
	jsonString, err := json.Marshal(m)
	if err != nil {
		return nil, err
	}
	return jsonString, nil
}

func NewHub() *Hub {
	return &Hub{
		clients:    make(map[int]*Client),
		register:   make(chan *Client),
		unregister: make(chan *Client),
		broadcast:  make(chan *WrapMessage),
	}
}

func (h *Hub) Run() {
	for {
		select {
		case c := <-h.register:
			h.clients[c.id] = c
			fmt.Println(c)
			fmt.Println(len(h.clients))
		case c := <-h.unregister:
			h.unregisterClient(c)
		case message := <-h.broadcast:
			for _, c := range h.clients {
				select {
				case c.send <- message:
				default:
					h.unregisterClient(c)
				}
			}
		}
	}
}

func (h *Hub) unregisterClient(c *Client) {
	close(c.send)
	delete(h.clients, c.id)
}
