use actix_web::{test, App};
use rinha_backend_q1_2024::models::{TransactionRequest, AppState};
use rinha_backend_q1_2024::endpoints::post_transaction;

#[actix_rt::test]
async fn test_post_transaction() {
    // Create an instance of your application state
    let app_state = AppState {
        pool: /* create a test database pool here */,
    };

    // Create an Actix Web application with the app state
    let mut app = test::init_service(
            App::new()
            .app_data(app_state.clone())
            .service(post_transaction))
        .await;

    // Define the request body
    let transaction_request: TransactionRequest = TransactionRequest {
        // define transaction details
        tipo: 'c',
        valor: 20,
        descricao: "hey".to_string()
    };

    // Send a POST request to the handler function
    let req = test::TestRequest::post()
        .uri("/clientes/123/transacoes")
        .set_json(&transaction_request)
        .to_request();
    
    let resp = test::call_service(&mut app, req).await;

    // Assert the response
    assert!(resp.status().is_success());
}